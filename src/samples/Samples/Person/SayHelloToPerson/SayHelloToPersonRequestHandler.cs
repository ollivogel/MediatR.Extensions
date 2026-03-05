namespace Samples.Person.SayHelloToPerson;

using MediatR;

public class SayHelloToPersonRequestHandler : IRequestHandler<SayHelloToPersonRequest, SayHelloToPersonResponse>
{
  public async Task<SayHelloToPersonResponse> Handle(SayHelloToPersonRequest request,
    CancellationToken cancellationToken)
  {
    return new SayHelloToPersonResponse()
    {
      Response = request.MessageToPerson,
    };
  }
}
