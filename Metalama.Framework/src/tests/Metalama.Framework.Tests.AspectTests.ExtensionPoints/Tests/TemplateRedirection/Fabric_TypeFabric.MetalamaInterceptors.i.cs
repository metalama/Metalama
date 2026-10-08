internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
  {
    global::System.Console.WriteLine("Intercepting Compute_Interceptor.");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_TypeFabric.Source.Compute(x);
  }
}