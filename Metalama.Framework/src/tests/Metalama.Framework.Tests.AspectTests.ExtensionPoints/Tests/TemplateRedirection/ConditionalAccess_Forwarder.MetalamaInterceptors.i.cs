internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Length_Interceptor(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.ConditionalAccess_Forwarder.Text receiver)
  {
    global::System.Console.WriteLine("Intercepting Length.");
    return receiver.Length();
  }
}