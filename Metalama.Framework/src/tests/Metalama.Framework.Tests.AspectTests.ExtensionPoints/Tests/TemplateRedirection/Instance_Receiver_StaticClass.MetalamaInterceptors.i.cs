internal static class MetalamaInterceptors
{
  internal static global::System.String Greet_Interceptor(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Receiver_StaticClass.Greeter receiver, global::System.String name)
  {
    global::System.Console.WriteLine($"Greeting '{name}'.");
    return receiver.Greet(name);
  }
}