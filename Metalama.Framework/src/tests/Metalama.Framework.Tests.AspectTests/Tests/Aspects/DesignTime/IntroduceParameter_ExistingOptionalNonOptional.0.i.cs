namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptionalNonOptional
{
  partial class TestClass
  {
    public TestClass(global::System.Int32 param, global::System.Int32 optParam = 42, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced1 = 42, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.String introduced2 = "42") : this(param, optParam: optParam)
    {
    }
  }
}