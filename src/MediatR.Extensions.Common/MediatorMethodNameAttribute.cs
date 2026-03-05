namespace MediatR.Extensions.Common;

/// <summary>
/// Specifies a custom method name for the generated MediatR extension methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MediatorMethodNameAttribute : Attribute
{
  public string MethodName { get; }

  public MediatorMethodNameAttribute(string methodName)
  {
    MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
  }
}
