internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Subtract_Interceptor(global::System.Int32 a, global::System.Int32 b)
  {
    global::System.Console.WriteLine($"q = {a}, b = {b}");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Template_PositionalBinding_SkipsNameBound.Source.Subtract(a, b);
  }
}