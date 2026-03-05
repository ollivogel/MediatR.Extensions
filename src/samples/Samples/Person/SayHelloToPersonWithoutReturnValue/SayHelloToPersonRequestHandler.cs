namespace Samples.Person.SayHelloToPersonWithoutReturnValue;

using MediatR;

public class SayHelloToPersonRequestHandler : IRequestHandler<SayHelloToPersonWithoutReturnValueRequest>
{
  public SayHelloToPersonRequestHandler()
  {
  }

  public async Task Handle(SayHelloToPersonWithoutReturnValueRequest request,
    CancellationToken cancellationToken)
  {
    Console.WriteLine(request.Message);
  }
}
