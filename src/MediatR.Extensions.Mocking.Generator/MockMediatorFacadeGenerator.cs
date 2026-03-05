namespace MediatR.Extensions.Mocking.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MediatR.Extensions.Shared;
using Microsoft.CodeAnalysis;

[Generator]
public sealed class MockMediatorFacadeGenerator : IIncrementalGenerator
{
  private const string mockMediatorUsings =
    """
    using MediatR.Extensions.Mocking;
    using MediatR.Extensions.Mocking.Fluent;
    """;

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var projectNamespaceProvider =
      context.CompilationProvider.Select(static (compilation, _) => compilation.Assembly.Name);

    // 1) Get all RequestInfos from compilation + references
    var requestInfos = context.CompilationProvider.SelectMany(static (compilation, _) => GetRequestInfos(compilation));

    var requestInfosWithProject = requestInfos.Combine(projectNamespaceProvider);

    context.RegisterSourceOutput(
      requestInfosWithProject,
      static (spc, pair) =>
      {
        var info = pair.Left;
        var projectNamespace = pair.Right;
        GeneratePerRequestFacades(spc, info, projectNamespace);
      });

    // 2b) Collect all infos to generate group extensions
    var collected = requestInfos.Collect();
    var collectedWithProject = collected.Combine(projectNamespaceProvider);

    context.RegisterSourceOutput(
      collectedWithProject,
      static (spc, pair) =>
      {
        var infos = pair.Left;
        var projectNamespace = pair.Right;
        GenerateGroupExtensions(spc, infos, projectNamespace);
      });
  }

  private static IEnumerable<RequestInfo> GetRequestInfos(Compilation compilation)
  {
    // Current assembly (always scan, including nested types)
    foreach (var info in GetRequestInfosFromAssembly(compilation.Assembly))
    {
      yield return info;
    }

    // Referenced assemblies (filtered to exclude framework/utility assemblies)
    foreach (var assembly in MediatorRequestDiscovery.GetRelevantAssemblies(compilation))
    {
      foreach (var info in GetRequestInfosFromAssembly(assembly))
      {
        yield return info;
      }
    }
  }

  private static IEnumerable<RequestInfo> GetRequestInfosFromAssembly(IAssemblySymbol assembly)
  {
    foreach (var type in GetAllTypes(assembly.GlobalNamespace))
    {
      if (type.TypeKind != TypeKind.Class || type.IsAbstract)
      {
        continue;
      }

      // Find IRequest / IRequest<TResponse>
      var requestInterface = type.AllInterfaces.FirstOrDefault(i => i.Name == "IRequest");
      var shortName = MediatorRequestDiscovery.ExtractShortName(type.Name);
      if (requestInterface is null || shortName is null)
      {
        continue;
      }

      // Read group attribute (optional)
      string? groupName = MediatorRequestDiscovery.GetMediatorGroupName(type);

      // Read custom method name attribute (optional)
      string? customMethodName = MediatorRequestDiscovery.GetMediatorMethodName(type);

      if (requestInterface.TypeArguments.Length == 0)
      {
        // IRequest without TResponse
        yield return new RequestInfo(
          requestType: type,
          responseType: null,
          shortName: shortName,
          groupName: groupName,
          customMethodName: customMethodName);
      }
      else
      {
        if (requestInterface.TypeArguments[0] is not INamedTypeSymbol responseType)
        {
          continue;
        }

        yield return new RequestInfo(
          requestType: type,
          responseType: responseType,
          shortName: shortName,
          groupName: groupName,
          customMethodName: customMethodName);
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
  {
    foreach (var subNs in ns.GetNamespaceMembers())
    {
      foreach (var t in GetAllTypes(subNs))
        yield return t;
    }

    foreach (var type in ns.GetTypeMembers())
    {
      yield return type;

      foreach (var nested in GetNestedTypes(type))
      {
        yield return nested;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
  {
    foreach (var nested in type.GetTypeMembers())
    {
      yield return nested;

      foreach (var nested2 in GetNestedTypes(nested))
      {
        yield return nested2;
      }
    }
  }

  // ------------------------------------------------------------
  //  Per Request: Registrant + (for ungrouped) direct Extension
  // ------------------------------------------------------------

  private static void GeneratePerRequestFacades(SourceProductionContext context, RequestInfo info,
    string projectNamespace)
  {
    var requestType = info.RequestType;
    var responseType = info.ResponseType;

    var facadeNamespace = projectNamespace;
    var fullRequestTypeName = requestType.ToDisplayString();

    string source;
    var hintNameSuffix = info.GroupName is null ? "MediatR.Mocking.Registration" : "MediatR.Mocking.GroupRegistration";

    if (responseType is not null)
    {
      var fullResponseTypeName = responseType.ToDisplayString();

      source = BuildSourceWithReturnValue(
        info.GroupName is not null, facadeNamespace, info.ShortName, fullRequestTypeName, fullResponseTypeName);
    }
    else
    {
      source = BuildSourceWithoutReturnValue(
        info.GroupName is not null, facadeNamespace, info.ShortName, fullRequestTypeName);
    }

    var hintName = $"{hintNameSuffix}.{info.ShortName}.g.cs";
    context.AddSource(hintName, source);
  }

  private static string CreateRegistrationExtension(string shortName, bool handlerHasReturnValue) =>
    $$"""
      public static class HandlerConfigurationStoreExtensions_{{shortName}}
      {
        extension(HandlerConfigurationStore handlerConfigurationStore)
        {
          public {{shortName}}HandlerConfigurationStoreRegistrant{{(handlerHasReturnValue ? "Factory" : string.Empty)}} {{shortName}} => new(handlerConfigurationStore);
        }
      }
      """;

  private static string BuildSourceWithReturnValue(
    bool isGrouped,
    string facadeNamespace,
    string shortName,
    string fullRequestTypeName,
    string fullResponseTypeName)
  {
    return
      $$"""
          // <auto-generated />
          #nullable enable
          using System;
          using System.Linq;
          using System.Threading.Tasks;
          {{mockMediatorUsings}}

          namespace {FacadeNamespace};

          {{(isGrouped ? string.Empty : CreateRegistrationExtension(shortName, true))}}

          public sealed class {ShortName}HandlerConfigurationStoreRegistrantFactory
            : IHandlerConfigurationRegistrantFactory<{FullResponseTypeName}, {ShortName}HandlerConfigurationStoreRegistrant>
          {
            private readonly HandlerConfigurationStore handlerConfigurationStore;

            public {ShortName}HandlerConfigurationStoreRegistrantFactory(HandlerConfigurationStore handlerConfigurationStore)
            {
              this.handlerConfigurationStore = handlerConfigurationStore ?? throw new ArgumentNullException(nameof(handlerConfigurationStore));
            }

            /// <summary>
            /// Registers a mock that returns the specified static value.
            /// </summary>
            public {ShortName}HandlerConfigurationStoreRegistrant WithReturnValue({FullResponseTypeName} response)
            {
              return new {ShortName}HandlerConfigurationStoreRegistrant(response, this.handlerConfigurationStore);
            }

            /// <summary>
            /// Registers a mock that throws the specified exception instead of returning a value.
            /// The callback (if any) is still invoked before the exception is thrown.
            /// </summary>
            public {ShortName}HandlerConfigurationStoreRegistrant Throws(Exception exception)
            {
              ArgumentNullException.ThrowIfNull(exception);
              var registrant = new {ShortName}HandlerConfigurationStoreRegistrant(default!, this.handlerConfigurationStore);
              registrant.SetException(exception);
              return registrant;
            }

            /// <summary>
            /// Registers a mock that throws a new instance of <typeparamref name="TException"/>.
            /// </summary>
            public {ShortName}HandlerConfigurationStoreRegistrant Throws<TException>() where TException : Exception, new()
            {
              return Throws(new TException());
            }

            /// <summary>
            /// Registers a mock that computes the response dynamically based on the incoming request.
            /// The factory is called on every Send(), receiving the actual request object.
            /// </summary>
            public {ShortName}HandlerConfigurationStoreRegistrant Returns(Func<{FullRequestTypeName}, {FullResponseTypeName}> factory)
            {
              ArgumentNullException.ThrowIfNull(factory);
              var registrant = new {ShortName}HandlerConfigurationStoreRegistrant(default!, this.handlerConfigurationStore);
              registrant.SetResponseFactory(factory);
              return registrant;
            }

            /// <summary>
            /// Registers a mock that cycles through the given values on successive calls (round-robin).
            /// After the last value, it wraps around to the first.
            /// </summary>
            public {ShortName}HandlerConfigurationStoreRegistrant ReturnsSequence(params {FullResponseTypeName}[] responses)
            {
              if (responses is null || responses.Length == 0) throw new ArgumentException("At least one response is required.", nameof(responses));
              var registrant = new {ShortName}HandlerConfigurationStoreRegistrant(responses[0], this.handlerConfigurationStore);
              registrant.SetResponseSequence(responses);
              return registrant;
            }
          }

          public sealed class {ShortName}HandlerConfigurationStoreRegistrant : IRegistrant
          {
            private readonly HandlerConfigurationStore handlerConfigurationStore;
            private readonly HandlerConfigurationWithReturnValue<{FullRequestTypeName}, {FullResponseTypeName}> registration;

            public {ShortName}HandlerConfigurationStoreRegistrant({FullResponseTypeName} response, HandlerConfigurationStore handlerConfigurationStore)
            {
              this.handlerConfigurationStore = handlerConfigurationStore ?? throw new ArgumentNullException(nameof(handlerConfigurationStore));
              this.registration = new HandlerConfigurationWithReturnValue<{FullRequestTypeName}, {FullResponseTypeName}>(response);
              this.handlerConfigurationStore.Register<{FullRequestTypeName}>(this.registration);
            }

            /// <summary>
            /// Registers a callback that is invoked with the request object before the mock response is returned.
            /// Use this to capture or assert on the incoming request.
            /// </summary>
            public void WithCallback(Action<{FullRequestTypeName}> callback)
            {
              ArgumentNullException.ThrowIfNull(callback);
              this.registration.Callback = callback;
            }

            internal void SetException(Exception exception) => this.registration.ExceptionToThrow = exception;
            internal void SetResponseFactory(Func<{FullRequestTypeName}, {FullResponseTypeName}> factory) => this.registration.ResponseFactory = factory;
            internal void SetResponseSequence({FullResponseTypeName}[] responses) => this.registration.ResponseSequence = responses.Cast<object?>().ToArray();
          }
          """
        .Replace("{FacadeNamespace}", facadeNamespace)
        .Replace("{ShortName}", shortName)
        .Replace("{FullRequestTypeName}", fullRequestTypeName)
        .Replace("{FullResponseTypeName}", fullResponseTypeName);
  }

  private static string BuildSourceWithoutReturnValue(
    bool isGrouped,
    string facadeNamespace,
    string shortName,
    string fullRequestTypeName)
  {
    return
      $$"""
          // <auto-generated />
          #nullable enable
          using System;
          using System.Linq;
          using System.Threading.Tasks;
          {{mockMediatorUsings}}

          namespace {FacadeNamespace};

          {{(isGrouped ? string.Empty : CreateRegistrationExtension(shortName, false))}}

          public sealed class {ShortName}HandlerConfigurationStoreRegistrant : IRegistrant
          {
            private readonly HandlerConfigurationStore handlerConfigurationStore;
            private readonly HandlerConfiguration<{FullRequestTypeName}> registration;

            public {ShortName}HandlerConfigurationStoreRegistrant(HandlerConfigurationStore handlerConfigurationStore)
            {
              this.handlerConfigurationStore = handlerConfigurationStore ?? throw new ArgumentNullException(nameof(handlerConfigurationStore));
              this.registration = new HandlerConfiguration<{FullRequestTypeName}>();
              this.handlerConfigurationStore.Register<{FullRequestTypeName}>(registration);
            }

            /// <summary>
            /// Registers a callback that is invoked with the request object before the mock completes.
            /// Use this to capture or assert on the incoming request.
            /// </summary>
            public void WithCallback(Action<{FullRequestTypeName}> callback)
            {
              ArgumentNullException.ThrowIfNull(callback);
              this.registration.Callback = callback;
            }

            /// <summary>
            /// Configures this mock to throw the specified exception instead of completing normally.
            /// The callback (if any) is still invoked before the exception is thrown.
            /// </summary>
            public void Throws(Exception exception)
            {
              ArgumentNullException.ThrowIfNull(exception);
              this.registration.ExceptionToThrow = exception;
            }

            /// <summary>
            /// Configures this mock to throw a new instance of <typeparamref name="TException"/>.
            /// </summary>
            public void Throws<TException>() where TException : Exception, new()
            {
              Throws(new TException());
            }
          }
          """
        .Replace("{FacadeNamespace}", facadeNamespace)
        .Replace("{ShortName}", shortName)
        .Replace("{FullRequestTypeName}", fullRequestTypeName);
  }

  // ------------------------------------------------------------
  //  Group extensions (for all requests with GroupName != null)
  // ------------------------------------------------------------

  private static void GenerateGroupExtensions(
    SourceProductionContext context,
    ImmutableArray<RequestInfo> infos,
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
      GenerateMockGroupHierarchy(context, projectNamespace, rootGroup, hierarchy[rootGroup], rootGroup, isRoot: true);
    }
  }

  private static void GenerateMockGroupHierarchy(
    SourceProductionContext context,
    string projectNamespace,
    string groupName,
    GroupNode<RequestInfo> node,
    string fullPath,
    bool isRoot)
  {
    // Use fullPath to ensure unique class names for nested groups
    var className = $"{fullPath.Replace(".", string.Empty)}MockGroup";

    var sb = new StringBuilder();

    // Generate properties for registrants/factories in this node
    foreach (var info in node.Items.OrderBy(i => i.CustomMethodName ?? i.ShortName, StringComparer.Ordinal))
    {
      var propertyName = info.CustomMethodName ?? info.ShortName;
      var hasReturnValue = info.ResponseType is not null;
      sb.AppendLine(
        $$"""
                  public {{info.ShortName}}HandlerConfigurationStoreRegistrant{{(hasReturnValue ? "Factory" : string.Empty)}} {{propertyName}} => new(group.HandlerConfigurationStore);
          """);
    }

    // Generate child group properties
    var childProperties = new StringBuilder();
    var hasChildren = node.Children.Count > 0;

    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      var childClassName = $"{childFullPath.Replace(".", string.Empty)}MockGroup";
      childProperties.AppendLine($"    internal {childClassName} {childName} {{ get; }}");
    }

    var properties = sb.ToString().TrimEnd();
    var hasProperties = !string.IsNullOrWhiteSpace(properties);

    // Constructor with child initialization
    var constructorBody = new StringBuilder();
    constructorBody.AppendLine(
      "      this.HandlerConfigurationStore = handlerConfigurationStore ?? throw new ArgumentNullException(nameof(handlerConfigurationStore));");
    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      var childClassName = $"{childFullPath.Replace(".", string.Empty)}MockGroup";
      constructorBody.AppendLine($"      this.{childName} = new {childClassName}(handlerConfigurationStore);");
    }

    var classBody = new StringBuilder();
    classBody.AppendLine("    internal HandlerConfigurationStore HandlerConfigurationStore { get; }");

    if (hasChildren)
    {
      classBody.AppendLine();
      classBody.Append(childProperties.ToString().TrimEnd());
    }

    var source = $$"""
                   // <auto-generated />
                   #nullable enable
                   using System;
                   using System.Threading.Tasks;
                   {{mockMediatorUsings}}

                   namespace {{projectNamespace}};

                   {{(isRoot ? $$"""
                                 /// <summary>
                                 /// Extension: handlerConfigurationStore.{{groupName}}
                                 /// </summary>
                                 public static class HandlerConfigurationStoreExtensions_{{groupName}}
                                 {
                                     extension(HandlerConfigurationStore handlerConfigurationStore)
                                     {
                                         public {{className}} {{groupName}} => new(handlerConfigurationStore);
                                     }
                                 }
                                 """ : string.Empty)}}

                   /// <summary>
                   /// Group for {{fullPath}} mock registrations
                   /// </summary>
                   public sealed class {{className}}
                   {
                   {{classBody.ToString().TrimEnd()}}

                       internal {{className}}(HandlerConfigurationStore handlerConfigurationStore)
                       {
                   {{constructorBody.ToString().TrimEnd()}}
                       }
                   }

                   {{(hasProperties ? $$"""
                                        /// <summary>
                                        /// Extensions for the {{fullPath}} mock group
                                        /// </summary>
                                        public static class {{className}}Extensions
                                        {
                                            extension({{className}} group)
                                            {
                                        {{properties}}
                                            }
                                        }
                                        """ : string.Empty)}}
                   """;

    var hintName = $"HandlerConfigurationStoreGroupExtensions.{fullPath.Replace(".", "_")}.g.cs";
    context.AddSource(hintName, source);

    // Recursively generate child groups
    foreach (var childName in node.Children.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      var childFullPath = $"{fullPath}.{childName}";
      GenerateMockGroupHierarchy(context, projectNamespace, childName, node.Children[childName], childFullPath,
        isRoot: false);
    }
  }
}
