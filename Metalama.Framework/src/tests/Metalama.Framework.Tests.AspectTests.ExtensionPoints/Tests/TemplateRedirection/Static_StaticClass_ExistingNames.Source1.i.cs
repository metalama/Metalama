namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_ExistingNames
{
  internal static class Source1
  {
    internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
    {
      return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_ExistingNames.Source.Compute(x);
    }
  }
}