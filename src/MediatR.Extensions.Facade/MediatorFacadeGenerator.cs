namespace MediatR.Extensions.Facade;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MediatR.Extensions.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Source generator for MediatR Facade extensions (Send API).
/// Generates strongly-typed Send methods with grouping and parameter mapping.
/// </summary>
[Generator]
public sealed class MediatorFacadeGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var projectNamespaceProvider = context.CompilationProvider
      .Select(static (compilation, _) => compilation.Assembly.Name);

    // SyntaxProvider for types in the current project
    var localCandidates = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => MediatorRequestDiscovery.IsCandidateClass(node),
        transform: static (ctx, _) => MediatorRequestDiscovery.GetSemanticTarget(ctx))
      .Where(static m => m is not null)
      .Select(static (m, _) => m!);

    // CompilationProvider for referenced assemblies (once per compilation)
    var referencedCandidates = context.CompilationProvider
      .SelectMany(static (compilation, _) => MediatorRequestDiscovery.GetRequestInfosFromReferences(compilation));

    // Combine both sources
    var allCandidates = localCandidates.Collect()
      .Combine(referencedCandidates.Collect())
      .SelectMany((pair, _) =>
      {
        var local = pair.Left;
        var referenced = pair.Right;
        return local.Concat(referenced);
      });

    var requestInfosWithProject = allCandidates
      .Combine(projectNamespaceProvider);

    // Generate extensions directly for ungrouped requests
    context.RegisterSourceOutput(
      requestInfosWithProject,
      static (spc, tuple) =>
      {
        var info = tuple.Left;
        var projectNamespace = tuple.Right;

        if (info.GroupName is null)
        {
          GenerateUngroupedExtension(spc, info, projectNamespace);
        }
      });

    // Collect grouped requests and generate grouped extensions
    var collected = allCandidates.Collect();
    var collectedWithProject = collected.Combine(projectNamespaceProvider);

    context.RegisterSourceOutput(
      collectedWithProject,
      static (spc, pair) =>
      {
        var infos = pair.Left;
        var projectNamespace = pair.Right;
        GenerateGroupedExtensions(spc, infos, projectNamespace);
      });
  }

  // ------------------------------------------------------------
  //  Ungrouped Extensions
  // ------------------------------------------------------------

  private static void GenerateUngroupedExtension(
    SourceProductionContext context,
    MediatorRequestInfo info,
    string projectNamespace)
  {
    var requestType = info.RequestType;
    var responseType = info.ResponseType;
    var fullRequestTypeName = requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    var methodName = info.CustomMethodName ?? info.ShortName;

    // Extract facade parameters
    var facadeParameters = GetFacadeParameters(requestType, context);

    var parameters = BuildParameterList(info.ConstructorInfo, facadeParameters);
    var constructorCall = BuildConstructorCall(info.ConstructorInfo, fullRequestTypeName);
    var facadeInitialization = BuildFacadeParameterInitialization(facadeParameters);

    string returnType;
    string sendStatement;

    if (responseType is not null)
    {
      var fullResponseTypeName = responseType.ToDisplayString(MediatorRequestDiscovery.NullableFullyQualifiedFormat);
      returnType = $"global::System.Threading.Tasks.Task<{fullResponseTypeName}>";
      sendStatement = $"return await mediator.Send(request, cancellationToken);";
    }
    else
    {
      returnType = "global::System.Threading.Tasks.Task";
      sendStatement = $"await mediator.Send(request, cancellationToken);";
    }

    // Build XML documentation for parameters
    var paramDocs = new StringBuilder();
    if (!info.ConstructorInfo.Parameters.IsDefaultOrEmpty)
    {
      foreach (var p in info.ConstructorInfo.Parameters)
      {
        paramDocs.AppendLine(
          $"/// <param name=\"{p.Name}\">Constructor parameter for <see cref=\"{fullRequestTypeName}\"/>.</param>");
      }
    }

    // Facade parameter documentation
    foreach (var fp in facadeParameters)
    {
      paramDocs.AppendLine(
        $"/// <param name=\"{fp.ParameterName}\">Facade parameter for property <see cref=\"{fullRequestTypeName}.{fp.Property.Name}\"/>.</param>");
    }

    var returnDoc = responseType is not null
      ? "/// <returns>The response from the handler for <see cref=\"" + fullRequestTypeName + "\"/>.</returns>"
      : "";

    var xmlDoc = $$"""
                   /// <summary>
                   /// Sends a <see cref="{{fullRequestTypeName}}"/> request via MediatR.
                   /// </summary>
                   {{paramDocs.ToString().TrimEnd()}}{{(paramDocs.Length > 0 ? "\n        " : "")}}/// <param name="configure">Optional configuration action for the request instance.</param>
                   /// <param name="cancellationToken">Cancellation token.</param>
                   {{(string.IsNullOrEmpty(returnDoc) ? "" : returnDoc)}}
                   """;

    var source = $$"""
                   // <auto-generated />
                   #nullable enable

                   namespace {{projectNamespace}};

                   public static class MediatorExtensions_{{info.ShortName}}
                   {
                       extension(global::MediatR.IMediator mediator)
                       {
                           {{xmlDoc.TrimEnd()}}
                           public async {{returnType}} {{methodName}}(
                               {{parameters}}global::System.Action<{{fullRequestTypeName}}>? configure = null,
                               global::System.Threading.CancellationToken cancellationToken = default)
                           {
                               {{constructorCall}}
                               {{facadeInitialization}}configure?.Invoke(request);
                               {{sendStatement}}
                           }
                       }
                   }
                   """;

    var hintName = $"MediatorExtensions.{info.ShortName}.g.cs";
    context.AddSource(hintName, source);
  }

  // ------------------------------------------------------------
  //  Grouped Extensions
  // ------------------------------------------------------------

  /// <summary>
  /// Generates the group class name from the full path.
  /// Format: Mediator[GroupPath] (e.g. "MediatorPersons", "MediatorPersonsAdmin")
  /// </summary>
  private static string GetGroupClassName(string fullPath)
  {
    return $"Mediator{fullPath.Replace(".", "")}";
  }

  private static void GenerateGroupedExtensions(
    SourceProductionContext context,
    ImmutableArray<MediatorRequestInfo> infos,
    string projectNamespace)
  {
    if (infos.IsDefaultOrEmpty)
    {
      return;
    }

    var groupedRequests = infos
      .Where(i => i.GroupName is not null)
      .ToList();

    if (groupedRequests.Count == 0)
    {
      return;
    }

    // Build hierarchy
    var hierarchy = GroupHierarchyHelper.BuildHierarchy(groupedRequests, r => r.GroupName);

    // Generate root-level groups
    foreach (var rootGroup in hierarchy.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      GenerateGroupHierarchy(context, projectNamespace, rootGroup, hierarchy[rootGroup], rootGroup, isRoot: true);
    }
  }

  private static void GenerateGroupHierarchy(
    SourceProductionContext context,
    string projectNamespace,
    string groupName,
    GroupNode<MediatorRequestInfo> node,
    string fullPath,
    bool isRoot)
  {
    // Generate class name
    var className = GetGroupClassName(fullPath);

    var sb = new StringBuilder();

    // Generate request methods for this node
    foreach (var info in node.Items.OrderBy(i => i.CustomMethodName ?? i.ShortName, StringComparer.Ordinal))
    {
      var methodName = info.CustomMethodName ?? info.ShortName;
      var fullRequestTypeName = info.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      // Extract facade parameters
      var facadeParameters = GetFacadeParameters(info.RequestType, context);

      var parameters = BuildParameterList(info.ConstructorInfo, facadeParameters);
      var constructorCall = BuildConstructorCall(info.ConstructorInfo, fullRequestTypeName);
      var facadeInitialization = BuildFacadeParameterInitialization(facadeParameters);

      string returnType;
      string sendStatement;

      if (info.ResponseType is not null)
      {
        var fullResponseTypeName =
          info.ResponseType.ToDisplayString(MediatorRequestDiscovery.NullableFullyQualifiedFormat);
        returnType = $"global::System.Threading.Tasks.Task<{fullResponseTypeName}>";
        sendStatement = $"return await group.Mediator.Send(request, cancellationToken);";
      }
      else
      {
        returnType = "global::System.Threading.Tasks.Task";
        sendStatement = $"await group.Mediator.Send(request, cancellationToken);";
      }

      // Build XML documentation for parameters
      var methodParamDocs = new StringBuilder();
      if (!info.ConstructorInfo.Parameters.IsDefaultOrEmpty)
      {
        foreach (var p in info.ConstructorInfo.Parameters)
        {
          methodParamDocs.AppendLine(
            $"/// <param name=\"{p.Name}\">Constructor parameter for <see cref=\"{fullRequestTypeName}\"/>.</param>");
        }
      }

      // Facade parameter documentation
      foreach (var fp in facadeParameters)
      {
        methodParamDocs.AppendLine(
          $"/// <param name=\"{fp.ParameterName}\">Facade parameter for property <see cref=\"{fullRequestTypeName}.{fp.Property.Name}\"/>.</param>");
      }

      var methodReturnDoc = info.ResponseType is not null
        ? "/// <returns>The response from the handler for <see cref=\"" + fullRequestTypeName + "\"/>.</returns>"
        : "";

      var methodXmlDoc = $$"""
                                   /// <summary>
                                   /// Sends a <see cref="{{fullRequestTypeName}}"/> request via MediatR.
                                   /// </summary>
                                   {{methodParamDocs.ToString().TrimEnd()}}{{(methodParamDocs.Length > 0 ? "\n        " : "")}}/// <param name="configure">Optional configuration action for the request instance.</param>
                                   /// <param name="cancellationToken">Cancellation token.</param>
                                   {{(string.IsNullOrEmpty(methodReturnDoc) ? "" : methodReturnDoc)}}
                           """;

      sb.AppendLine(methodXmlDoc.TrimEnd());
      sb.AppendLine(
        $$"""
                  public async {{returnType}} {{methodName}}(
                      {{parameters}}global::System.Action<{{fullRequestTypeName}}>? configure = null,
                      global::System.Threading.CancellationToken cancellationToken = default)
                  {
                      {{constructorCall}}
                      {{facadeInitialization}}configure?.Invoke(request);
                      {{sendStatement}}
                  }

          """);
    }

    // Generate child group properties
    var childProperties = new StringBuilder();
    var hasChildren = node.Children.Count > 0;

    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      var childClassName = GetGroupClassName(childFullPath);
      childProperties.AppendLine($"    public {childClassName} {childName} {{ get; }}");
    }

    var extensionMethods = sb.ToString().TrimEnd();
    var hasExtensions = !string.IsNullOrWhiteSpace(extensionMethods);

    // Constructor with child initialization
    var constructorBody = new StringBuilder();
    constructorBody.AppendLine(
      "        this.Mediator = mediator ?? throw new global::System.ArgumentNullException(nameof(mediator));");
    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      var childClassName = GetGroupClassName(childFullPath);
      constructorBody.AppendLine($"        this.{childName} = new {childClassName}(mediator);");
    }

    var classBody = new StringBuilder();
    classBody.AppendLine("    public global::MediatR.IMediator Mediator { get; }");

    if (hasChildren)
    {
      classBody.AppendLine();
      classBody.Append(childProperties.ToString().TrimEnd());
    }

    var source = $$"""
                   // <auto-generated />
                   #nullable enable

                   namespace {{projectNamespace}};

                   /// <summary>
                   /// Group for {{fullPath}} requests
                   /// </summary>
                   public class {{className}}
                   {
                   {{classBody.ToString().TrimEnd()}}

                       public {{className}}(global::MediatR.IMediator mediator)
                       {
                   {{constructorBody.ToString().TrimEnd()}}
                       }
                   }

                   {{(isRoot ? $$"""
                                 /// <summary>
                                 /// Extension: mediator.{{groupName}}
                                 /// </summary>
                                 public static class MediatorExtensions_{{groupName}}Group
                                 {
                                     extension(global::MediatR.IMediator mediator)
                                     {
                                         public {{className}} {{groupName}} => new(mediator);
                                     }
                                 }
                                 """ : string.Empty)}}

                   {{(hasExtensions ? $$"""
                                        /// <summary>
                                        /// Extensions for the {{fullPath}} group
                                        /// </summary>
                                        public static class {{className}}Extensions
                                        {
                                            extension({{className}} group)
                                            {
                                        {{extensionMethods}}
                                            }
                                        }
                                        """ : string.Empty)}}
                   """;

    var hintName = $"MediatorExtensions.{className}.g.cs";
    context.AddSource(hintName, source);

    // Recursively generate child groups
    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      GenerateGroupHierarchy(context, projectNamespace, childName, node.Children[childName], childFullPath,
        isRoot: false);
    }
  }

  // ------------------------------------------------------------
  //  Helper methods for parameters and constructor calls
  // ------------------------------------------------------------

  private static string BuildParameterList(ConstructorInfo constructorInfo, List<FacadeParameterInfo> facadeParameters)
  {
    var allParams = new List<string>();

    // Constructor parameters first (always required)
    if (!constructorInfo.Parameters.IsDefaultOrEmpty)
    {
      allParams.AddRange(constructorInfo.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
    }

    // Then facade parameters (sorted by Order, then Name)
    foreach (var fp in facadeParameters)
    {
      var paramType = fp.Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      if (fp.IsNullable && !paramType.EndsWith("?"))
      {
        paramType += "?";
      }

      if (fp.IsOptional)
      {
        var defaultValueStr = FormatDefaultValue(fp.DefaultValue);
        allParams.Add($"{paramType} {fp.ParameterName} = {defaultValueStr}");
      }
      else
      {
        allParams.Add($"{paramType} {fp.ParameterName}");
      }
    }

    if (allParams.Count == 0)
    {
      return string.Empty;
    }

    return string.Join(", ", allParams) + ", ";
  }

  private static string FormatDefaultValue(object? defaultValue) =>
    defaultValue switch
    {
      null => "null",
      string s when s == "null" => "null",
      string str => $"\"{str}\"",
      bool b => b ? "true" : "false",
      _ => defaultValue.ToString() ?? "default"
    };

  private static string BuildFacadeParameterInitialization(List<FacadeParameterInfo> facadeParameters)
  {
    if (facadeParameters.Count == 0)
    {
      return string.Empty;
    }

    var sb = new StringBuilder();
    foreach (var fp in facadeParameters)
    {
      sb.AppendLine($"request.{fp.Property.Name} = {fp.ParameterName};");
      sb.Append("               ");
    }

    return sb.ToString();
  }

  private static string BuildConstructorCall(ConstructorInfo constructorInfo, string fullRequestTypeName)
  {
    if (constructorInfo.Parameters.IsDefaultOrEmpty)
    {
      return $"var request = new {fullRequestTypeName}();";
    }

    var arguments = string.Join(", ", constructorInfo.Parameters.Select(p => p.Name));
    return $"var request = new {fullRequestTypeName}({arguments});";
  }

  private static List<FacadeParameterInfo> GetFacadeParameters(INamedTypeSymbol requestType,
    SourceProductionContext context)
  {
    var facadeParams = requestType.GetMembers()
      .OfType<IPropertySymbol>()
      .Where(p => p.DeclaredAccessibility == Accessibility.Public)
      .Select(p => new
      {
        Property = p,
        Attribute = p.GetAttributes().FirstOrDefault(a =>
          a.AttributeClass?.Name is "FacadeParameterAttribute" or "FacadeParameter")
      })
      .Where(x => x.Attribute is not null)
      .Select(x =>
      {
        var attr = x.Attribute!;
        var property = x.Property;

        // Extract attribute values
        var nameArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Name");
        var paramName = nameArg.Value.Value as string;
        if (string.IsNullOrEmpty(paramName))
        {
          // Fallback: camelCase property name
          paramName = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        }

        var orderArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Order");
        var order = orderArg.Value.Value is int o ? o : 0;

        var isOptionalArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "IsOptional");
        var defaultValueArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "DefaultValue");

        // Check if property is nullable
        var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                         (property.Type is INamedTypeSymbol namedType &&
                          namedType.IsGenericType &&
                          namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);

        // IsOptional: explicitly set or auto-detect for nullable
        bool isOptional;
        if (isOptionalArg.Value.Value is bool explicitOptional)
        {
          isOptional = explicitOptional;

          // Warning: IsOptional=false but property is nullable
          if (!explicitOptional && isNullable)
          {
            context.ReportDiagnostic(Diagnostic.Create(
              new DiagnosticDescriptor(
                "MEDIATOR001",
                "Facade parameter marked as required but property is nullable",
                "Property '{0}' on '{1}' is nullable but FacadeParameter has IsOptional=false. Consider making it required or removing IsOptional=false.",
                "MediatR.Facade",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
              property.Locations.FirstOrDefault(),
              property.Name,
              requestType.Name));
          }
        }
        else
        {
          // Auto-detect: nullable = optional
          isOptional = isNullable;
        }

        // DefaultValue
        var defaultValue = defaultValueArg.Value.Value;
        if (isOptional && defaultValue == null && isNullable)
        {
          defaultValue = "null";
        }

        return new FacadeParameterInfo(property, paramName, order, isOptional, defaultValue, isNullable);
      })
      .ToList();

    // Validation: Order must be unique
    var orderGroups = facadeParams
      .GroupBy(p => p.Order)
      .Where(g => g.Count() > 1)
      .ToList();

    if (orderGroups.Any())
    {
      foreach (var group in orderGroups)
      {
        foreach (var param in group)
        {
          context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
              "MEDIATOR002",
              "Duplicate FacadeParameter Order",
              "Multiple FacadeParameters on '{0}' have the same Order value {1}. Each FacadeParameter must have a unique Order value.",
              "MediatR.Facade",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true),
            param.Property.Locations.FirstOrDefault(),
            requestType.Name,
            param.Order));
        }
      }
    }

    // Sort: first by Order, then by ParameterName
    return facadeParams
      .OrderBy(p => p.Order)
      .ThenBy(p => p.ParameterName)
      .ToList();
  }

  private record FacadeParameterInfo(
    IPropertySymbol Property,
    string ParameterName,
    int Order,
    bool IsOptional,
    object? DefaultValue,
    bool IsNullable);
}
