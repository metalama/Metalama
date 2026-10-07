internal static class MetalamaInterceptors
{
  internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
  {
    global::System.Console.WriteLine("[log] declared for 'Source.Compute( 1 )'");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.CompileTimeArgument_And_MetaExtension.Source.Compute(x);
  }
  internal static global::System.Int32 Compute_Interceptor1(global::System.Int32 x)
  {
    global::System.Console.WriteLine("[log] declared for 'Source.Compute( 2 )'");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.CompileTimeArgument_And_MetaExtension.Source.Compute(x);
  }
}