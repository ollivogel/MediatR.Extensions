namespace MediatR.Extensions.Common;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MediatorGroupAttribute : Attribute
{
  public string GroupName { get; }

  public MediatorGroupAttribute(string groupName)
  {
    GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
  }
}
