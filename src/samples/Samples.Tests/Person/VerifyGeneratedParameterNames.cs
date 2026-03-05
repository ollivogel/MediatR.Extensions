namespace Samples.Tests.Person;

using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Verifies that the code generator for BuilderMethod attributes
/// makes ParameterName optional and automatically uses the
/// property name in camelCase when no value is specified.
/// </summary>
public class VerifyGeneratedParameterNames
{
  private readonly ITestOutputHelper output;

  public VerifyGeneratedParameterNames(ITestOutputHelper output)
  {
    this.output = output;
  }

  [Fact]
  public void GeneratedBuilder_Should_UseCamelCaseFallback_WhenParameterNameNotSpecified()
  {
    // Arrange: find the generated builder class
    var builderType = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(a => a.GetTypes())
      .FirstOrDefault(t => t.Name == "CreateUserCommandBuilder");

    builderType.Should().NotBeNull("CreateUserCommandBuilder should be generated");

    // Act: find the WithName method
    var withNameMethod = builderType!.GetMethod("WithName");
    withNameMethod.Should().NotBeNull("WithName method should exist");

    var parameters = withNameMethod!.GetParameters();

    // Assert
    parameters.Should().HaveCount(2, "WithName should have 2 parameters");

    // Test 1: first parameter should be "firstName"
    // Property: FirstName
    // Attribute: [BuilderMethod("WithName", Order = 0)]  <- NO ParameterName!
    // Expected: camelCase conversion from "FirstName" -> "firstName"
    parameters[0].Name.Should().Be("firstName",
      "First parameter should use camelCase fallback from property name 'FirstName' when ParameterName is not specified");

    // Test 2: second parameter should be "thisIsTheLastName"
    // Property: LastName
    // Attribute: [BuilderMethod("WithName", ParameterName = "thisIsTheLastName", Order = 1)]
    // Expected: the explicitly specified value
    parameters[1].Name.Should().Be("thisIsTheLastName",
      "Second parameter should use the explicitly specified ParameterName value");

    // Output for debugging
    output.WriteLine($"✓ Parameter 1: {parameters[0].Name} (camelCase fallback from 'FirstName')");
    output.WriteLine($"✓ Parameter 2: {parameters[1].Name} (explicit ParameterName)");
  }
}
