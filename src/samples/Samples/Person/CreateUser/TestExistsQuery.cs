namespace Samples.Person.CreateUser;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Test.Exists")]
[MediatorMethodName("ByName")]
public class TestExistsQuery : IRequest<int>
{
}

[MediatorGroup("AnotherTest.Exists")]
[MediatorMethodName("ByName")]
public class AnotherTestExistsQuery : IRequest<int>
{
}
