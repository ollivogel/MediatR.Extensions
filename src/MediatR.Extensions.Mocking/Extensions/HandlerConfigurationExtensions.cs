namespace MediatR.Extensions.Mocking.Extensions;

public static class HandlerConfigurationExtensions
{
  extension(HandlerConfigurationBase)
  {
    /// <summary>
    /// Creates a default handler configuration for the given request type using reflection.
    /// For requests with return values, creates a <see cref="HandlerConfigurationWithReturnValue{TRequest,TResponse}"/>
    /// with a default-constructed response. For void requests, creates a <see cref="HandlerConfiguration{TRequest}"/>.
    /// </summary>
    public static HandlerConfigurationBase CreateDefault(Type requestType)
    {
      var iRequestInterface = requestType
        .GetInterfaces()
        .FirstOrDefault(i =>
          i.IsGenericType &&
          i.GetGenericTypeDefinition() == typeof(IRequest<>));

      var hasReturnValue = iRequestInterface is not null;

      if (hasReturnValue)
      {
        var responseType = iRequestInterface!.GetGenericArguments()[0];

        var handlerConfigurationWithReturnValueType =
          typeof(HandlerConfigurationWithReturnValue<,>).MakeGenericType(requestType, responseType);

        var defaultResponseInstance = CreateEnsuredInstance(responseType);

        var handlerConfigurationWithReturnValueInstance =
          CreateEnsuredInstance(handlerConfigurationWithReturnValueType, defaultResponseInstance);

        return (HandlerConfigurationBase)handlerConfigurationWithReturnValueInstance;
      }

      var handlerConfigurationType = typeof(HandlerConfiguration<>).MakeGenericType(requestType);
      var handlerConfigurationInstance = CreateEnsuredInstance(handlerConfigurationType);

      return (HandlerConfigurationBase)handlerConfigurationInstance;
    }

    private static object CreateEnsuredInstance(Type instanceType, params object[] parameters)
    {
      var instance = Activator.CreateInstance(instanceType, parameters);
      if (instance == null)
      {
        throw new InvalidOperationException(
          $"Could not create instance of '{instanceType.FullName}'.");
      }

      return instance;
    }
  }
}
