namespace MediatR.Extensions.Mocking.Generator;

using Microsoft.CodeAnalysis;

internal sealed class RequestInfo
{
  public RequestInfo(INamedTypeSymbol requestType, INamedTypeSymbol? responseType, string shortName, string? groupName,
    string? customMethodName = null)
  {
    RequestType = requestType;
    ResponseType = responseType;
    ShortName = shortName;
    GroupName = groupName;
    CustomMethodName = customMethodName;
  }

  public INamedTypeSymbol RequestType { get; }
  public INamedTypeSymbol? ResponseType { get; }
  public string ShortName { get; }
  public string? GroupName { get; }
  public string? CustomMethodName { get; }
}
