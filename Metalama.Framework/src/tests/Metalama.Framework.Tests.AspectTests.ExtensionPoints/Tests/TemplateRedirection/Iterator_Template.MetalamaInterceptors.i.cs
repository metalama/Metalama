internal static class MetalamaInterceptors
{
  internal static global::System.Collections.Generic.IEnumerable<global::System.Int32> Range_Interceptor(global::System.Int32 count)
  {
    foreach (var item in (global::System.Collections.Generic.IEnumerable<global::System.Int32>)global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Iterator_Template.Source.Range(count))
    {
      global::System.Console.WriteLine($"Yielding {item}.");
      yield return item;
    }
  }
}