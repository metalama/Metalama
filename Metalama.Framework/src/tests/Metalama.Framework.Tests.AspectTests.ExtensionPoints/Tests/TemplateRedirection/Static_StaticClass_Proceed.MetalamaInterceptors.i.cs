internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
  {
    global::System.Console.WriteLine("Intercepting Compute_Interceptor with 1 parameter(s).");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_Proceed.Source.Compute(x);
  }
}