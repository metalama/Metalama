internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
  {
    global::System.Console.WriteLine($"{(string)null} {3} {5L} {((global::System.DayOfWeek)(1))} {default(global::System.DateTime).Ticks}");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Template_DefaultValueParameter.Source.Compute(x);
  }
}