namespace MediatR.Extensions.Generator.Tests;

using MediatR.Extensions.Mocking.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public class FacadeGeneratorTests
{
  [Fact]
  public void Generator_Creates_Code_For_Requests_With_Return_Value()
  {
    var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    var request = """
                  using MediatR;

                  public class SayHelloToPersonRequest : IRequest<SayHelloToPersonResponse>;
                  """;

    var response = """
                   public class SayHelloToPersonResponse
                   {
                     public string Response { get; set; }
                   }
                   """;

    var handler = """
                  using MediatR;

                  public class SayHelloToPersonRequestHandler : IRequestHandler<SayHelloToPersonRequest, SayHelloToPersonResponse>
                  {
                    public async Task<SayHelloToPersonResponse> Handle(SayHelloToPersonRequest request,
                      CancellationToken cancellationToken)
                    {
                      return new SayHelloToPersonResponse()
                      {
                        Response = "Supernova",
                      };
                    }
                  }
                  """;

    var trees = new[]
    {
      CSharpSyntaxTree.ParseText(request, parseOptions),
      CSharpSyntaxTree.ParseText(response, parseOptions),
      CSharpSyntaxTree.ParseText(handler, parseOptions),
    };

    var refs = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
    };

    var compilation = CSharpCompilation.Create(
      "Generator.Smoke",
      trees,
      refs,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new MockMediatorFacadeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: [generator.AsSourceGenerator()],
      additionalTexts: null,
      parseOptions: parseOptions,
      optionsProvider: null);

    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out var updatedCompilation,
      out var generatorDiagnostics);

    var runResult = driver.GetRunResult();
    Assert.DoesNotContain(generatorDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
    Assert.DoesNotContain(runResult.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    Assert.True(runResult.GeneratedTrees.Length > 0);

    var tree = runResult.GeneratedTrees.Single();
    var generated = tree.GetText().ToString();
    Assert.NotEmpty(generated);
  }

  [Fact]
  public void Generator_Creates_Code_For_Requests_Without_Return_Value()
  {
    var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    var request =
      """
      using MediatR;

      [MediatorGroup("Persons")]
      public class SayHelloToPersonRequest : IRequest<bool>;
      """;

    var attribute =
      """
      using System;

      [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class MediatorGroupAttribute : Attribute
      {
        public string GroupName { get; }

        public MediatorGroupAttribute(string groupName)
        {
          GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        }
      }
      """;

    var trees = new[]
    {
      CSharpSyntaxTree.ParseText(request, parseOptions),
      CSharpSyntaxTree.ParseText(attribute, parseOptions),
    };

    var refs = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
    };

    var compilation = CSharpCompilation.Create(
      "Generator.Smoke",
      trees,
      refs,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new MockMediatorFacadeGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: [generator.AsSourceGenerator()],
      additionalTexts: null,
      parseOptions: parseOptions,
      optionsProvider: null);

    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out var updatedCompilation,
      out var generatorDiagnostics);

    var runResult = driver.GetRunResult();
    Assert.DoesNotContain(generatorDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
    Assert.DoesNotContain(runResult.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    Assert.True(runResult.GeneratedTrees.Length > 0);

    var generatedCode = runResult.GeneratedTrees.Select(t => t.GetText().ToString()).ToList();
    Assert.All(generatedCode, code => Assert.NotEmpty(code));
  }
}
