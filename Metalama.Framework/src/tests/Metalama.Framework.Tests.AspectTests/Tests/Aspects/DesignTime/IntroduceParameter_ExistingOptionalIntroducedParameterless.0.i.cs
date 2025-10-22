namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_ExistingOptionalIntroducedParameterless
{
  partial class TestClass
  {
    public TestClass(global::System.Int32 x)
    {
    }
    public TestClass(global::System.Int32 param, global::System.Int32 optParam, global::System.Int32 introduced1, global::System.String introduced2 = "42") : this(param, optParam: optParam)
    {
    }
  }
}