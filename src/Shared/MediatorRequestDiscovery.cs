namespace MediatR.Extensions.Shared;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Shared logic for request discovery used by multiple generators.
/// </summary>
internal static class MediatorRequestDiscovery
{
  /// <summary>
  /// FullyQualifiedFormat that includes the ? suffix for nullable reference types.
  /// Use this instead of SymbolDisplayFormat.FullyQualifiedFormat when generating type names.
  /// </summary>
  public static readonly SymbolDisplayFormat NullableFullyQualifiedFormat =
    SymbolDisplayFormat.FullyQualifiedFormat
      .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  private static readonly string[] MicrosoftPublicKeyTokens =
  [
    "b77a5c561934e089", // Microsoft
    "b03f5f7f11d50a3a", // Microsoft
    "31bf3856ad364e35", // Microsoft .NET Framework
    "cc7b13ffcd2ddd51", // Microsoft
    "adb9793829ddae60" // Microsoft
  ];
  // ------------------------------------------------------------
  //  Syntax filtering (fast, no semantics)
  // ------------------------------------------------------------

  /// <summary>
  /// Fast syntactic filtering: checks if a SyntaxNode is a candidate for a MediatR request.
  /// </summary>
  public static bool IsCandidateClass(SyntaxNode node)
  {
    // Only class declarations
    if (node is not ClassDeclarationSyntax classDecl)
    {
      return false;
    }

    // Quick check: name ends with Request, Command, or Query
    var className = classDecl.Identifier.ValueText;
    return className.EndsWith("Request", StringComparison.Ordinal) ||
           className.EndsWith("Command", StringComparison.Ordinal) ||
           className.EndsWith("Query", StringComparison.Ordinal);
  }

  // ------------------------------------------------------------
  //  Assembly filtering (exclude framework assemblies)
  // ------------------------------------------------------------

  /// <summary>
  /// Filters referenced assemblies, excluding framework assemblies.
  /// </summary>
  public static IEnumerable<IAssemblySymbol> GetRelevantAssemblies(Compilation compilation)
  {
    return compilation.SourceModule.ReferencedAssemblySymbols
      .Where(a =>
      {
        // 1. Check if assembly has a Microsoft public key token
        if (!a.Identity.PublicKeyToken.IsDefaultOrEmpty)
        {
          var publicKeyToken = BitConverter.ToString(a.Identity.PublicKeyToken.ToArray()).Replace("-", "")
            .ToLowerInvariant();

          if (MicrosoftPublicKeyTokens.Contains(publicKeyToken))
          {
            return false; // Framework assembly
          }
        }

        var name = a.Name;

        // 2. Known framework prefixes
        if (name.StartsWith("System", StringComparison.Ordinal) ||
            name.StartsWith("netstandard", StringComparison.Ordinal) ||
            name.Equals("mscorlib", StringComparison.Ordinal))
        {
          return false;
        }

        // 3. MediatR itself
        if (name == "MediatR")
        {
          return false;
        }

        // 4. Known test frameworks
        if (name.StartsWith("xunit", StringComparison.Ordinal) ||
            name.StartsWith("nunit", StringComparison.Ordinal) ||
            name.StartsWith("MSTest", StringComparison.Ordinal) ||
            name.StartsWith("Moq", StringComparison.Ordinal) ||
            name.StartsWith("NSubstitute", StringComparison.Ordinal) ||
            name.StartsWith("FluentAssertions", StringComparison.Ordinal))
        {
          return false;
        }

        // 5. Known utility libraries
        if (name.StartsWith("Newtonsoft.Json", StringComparison.Ordinal) ||
            name.StartsWith("AutoMapper", StringComparison.Ordinal) ||
            name.StartsWith("Serilog", StringComparison.Ordinal) ||
            name.StartsWith("NLog", StringComparison.Ordinal))
        {
          return false;
        }

        return true;
      });
  }

  /// <summary>
  /// Searches an assembly for all types (non-recursive, using stack).
  /// </summary>
  public static IEnumerable<INamedTypeSymbol> GetTypesFromAssembly(IAssemblySymbol assembly)
  {
    var stack = new Stack<INamespaceSymbol>();
    stack.Push(assembly.GlobalNamespace);

    while (stack.Count > 0)
    {
      var ns = stack.Pop();

      foreach (var type in ns.GetTypeMembers())
      {
        if (type.TypeKind == TypeKind.Class && !type.IsAbstract)
        {
          yield return type;
        }
      }

      foreach (var childNs in ns.GetNamespaceMembers())
      {
        stack.Push(childNs);
      }
    }
  }

  // ------------------------------------------------------------
  //  Attribute parsing (shared logic)
  // ------------------------------------------------------------

  /// <summary>
  /// Extracts the MediatorGroup name from a type.
  /// </summary>
  public static string? GetMediatorGroupName(INamedTypeSymbol type)
  {
    var attr = type.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.Name is "MediatorGroupAttribute" or "MediatorGroup");

    if (attr is null)
    {
      return null;
    }

    if (attr.ConstructorArguments.Length == 0)
    {
      return null;
    }

    return attr.ConstructorArguments[0].Value as string;
  }

  /// <summary>
  /// Extracts the custom method name from a type.
  /// </summary>
  public static string? GetMediatorMethodName(INamedTypeSymbol type)
  {
    var attr = type.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.Name is "MediatorMethodNameAttribute" or "MediatorMethodName");

    if (attr is null)
    {
      return null;
    }

    if (attr.ConstructorArguments.Length == 0)
    {
      return null;
    }

    return attr.ConstructorArguments[0].Value as string;
  }

  /// <summary>
  /// Checks if a type has the GenerateFluentBuilder attribute.
  /// </summary>
  public static bool HasGenerateFluentBuilderAttribute(INamedTypeSymbol type)
  {
    return type.GetAttributes()
      .Any(a => a.AttributeClass?.Name is "GenerateFluentBuilderAttribute" or "GenerateFluentBuilder");
  }

  // ------------------------------------------------------------
  //  Full request discovery pipeline (shared by Facade + FluentBuilder)
  // ------------------------------------------------------------

  /// <summary>
  /// Resolves a class symbol from a <see cref="GeneratorSyntaxContext"/> and creates a <see cref="MediatorRequestInfo"/>.
  /// </summary>
  public static MediatorRequestInfo? GetSemanticTarget(GeneratorSyntaxContext context)
  {
    var classDecl = (ClassDeclarationSyntax)context.Node;

    if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
    {
      return null;
    }

    return CreateRequestInfo(classSymbol);
  }

  /// <summary>
  /// Discovers all MediatR request types from referenced assemblies.
  /// </summary>
  public static IEnumerable<MediatorRequestInfo> GetRequestInfosFromReferences(Compilation compilation)
  {
    foreach (var assembly in GetRelevantAssemblies(compilation))
    {
      foreach (var type in GetTypesFromAssembly(assembly))
      {
        if (!type.Name.EndsWith("Request", StringComparison.Ordinal) &&
            !type.Name.EndsWith("Command", StringComparison.Ordinal) &&
            !type.Name.EndsWith("Query", StringComparison.Ordinal))
        {
          continue;
        }

        var info = CreateRequestInfo(type);
        if (info is not null)
        {
          yield return info;
        }
      }
    }
  }

  /// <summary>
  /// Creates a <see cref="MediatorRequestInfo"/> from a class symbol, or null if it's not a valid MediatR request.
  /// </summary>
  public static MediatorRequestInfo? CreateRequestInfo(INamedTypeSymbol classSymbol)
  {
    if (classSymbol.IsAbstract || classSymbol.TypeKind != TypeKind.Class)
    {
      return null;
    }

    var requestInterface = classSymbol.AllInterfaces
      .FirstOrDefault(i => i.Name == "IRequest" &&
                           i.ContainingNamespace?.ToDisplayString() == "MediatR");

    if (requestInterface is null)
    {
      return null;
    }

    var groupName = GetMediatorGroupName(classSymbol);
    var customMethodName = GetMediatorMethodName(classSymbol);
    var generateFluentBuilder = HasGenerateFluentBuilderAttribute(classSymbol);

    var shortName = ExtractShortName(classSymbol.Name);
    if (shortName is null)
    {
      return null;
    }

    var constructorInfo = GetBestConstructor(classSymbol);

    var responseType = requestInterface.TypeArguments.Length > 0
      ? requestInterface.TypeArguments[0] as INamedTypeSymbol
      : null;

    if (requestInterface.TypeArguments.Length > 0 && responseType is null)
    {
      return null;
    }

    return new MediatorRequestInfo(
      classSymbol,
      responseType,
      shortName,
      groupName,
      constructorInfo,
      customMethodName,
      generateFluentBuilder);
  }

  // ------------------------------------------------------------
  //  Constructor extraction
  // ------------------------------------------------------------

  /// <summary>
  /// Selects the "best" constructor: fewest parameters (but not parameterless if others exist).
  /// </summary>
  public static ConstructorInfo GetBestConstructor(INamedTypeSymbol type)
  {
    var constructors = type.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .ToList();

    if (constructors.Count == 0)
    {
      return new ConstructorInfo(null, ImmutableArray<ParameterInfo>.Empty);
    }

    var parameterlessConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
    var constructorsWithParameters = constructors.Where(c => c.Parameters.Length > 0).ToList();

    IMethodSymbol? selectedConstructor;

    if (constructorsWithParameters.Count > 0)
    {
      selectedConstructor = constructorsWithParameters.OrderBy(c => c.Parameters.Length).First();
    }
    else
    {
      selectedConstructor = parameterlessConstructor;
    }

    if (selectedConstructor is null)
    {
      return new ConstructorInfo(null, ImmutableArray<ParameterInfo>.Empty);
    }

    var parameters = selectedConstructor.Parameters
      .Select(p => new ParameterInfo(
        p.Name,
        p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
      .ToImmutableArray();

    return new ConstructorInfo(selectedConstructor, parameters);
  }

  /// <summary>
  /// Returns all public constructors (sorted by parameter count).
  /// </summary>
  public static ImmutableArray<ConstructorInfo> GetAllPublicConstructors(INamedTypeSymbol type)
  {
    var constructors = type.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .OrderBy(c => c.Parameters.Length)
      .Select(ctor => new ConstructorInfo(
        ctor,
        ctor.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
          .ToImmutableArray()))
      .ToImmutableArray();

    return constructors;
  }

  // ------------------------------------------------------------
  //  Short Name Extraction
  // ------------------------------------------------------------

  /// <summary>
  /// Extracts the short name (without "Request"/"Command"/"Query" suffix).
  /// </summary>
  public static string? ExtractShortName(string className)
  {
    string[] endings = ["Request", "Command", "Query"];
    foreach (var ending in endings)
    {
      if (className.EndsWith(ending, StringComparison.Ordinal))
      {
        return className[..^ending.Length];
      }
    }

    return null;
  }
}

// ------------------------------------------------------------
//  Helper classes (outside the static class)
// ------------------------------------------------------------

internal sealed record ConstructorInfo(
  IMethodSymbol? Constructor,
  ImmutableArray<ParameterInfo> Parameters);

internal sealed record ParameterInfo(
  string Name,
  string TypeName);

/// <summary>
/// Shared request info used by both generators.
/// </summary>
internal sealed record MediatorRequestInfo(
  INamedTypeSymbol RequestType,
  INamedTypeSymbol? ResponseType,
  string ShortName,
  string? GroupName,
  ConstructorInfo ConstructorInfo,
  string? CustomMethodName,
  bool GenerateFluentBuilder = false);
