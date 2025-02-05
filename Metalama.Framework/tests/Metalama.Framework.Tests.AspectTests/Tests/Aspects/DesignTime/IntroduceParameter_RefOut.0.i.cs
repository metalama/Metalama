namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_RefOut
{
  partial class TestClass
  {
    public TestClass(ref global::System.Int32 param, global::System.Int32 optParam = 42, global::System.Int32 introduced1 = 42, global::System.String introduced2 = "42") : this(ref param, optParam: optParam)
    {
    }
    public TestClass(ref global::System.Int32 param) : this(ref param, optParam: default(global::System.Int32))
    {
    }
    public TestClass(out global::System.String param, global::System.Int32 optParam = 42, global::System.Int32 introduced1 = 42, global::System.String introduced2 = "42") : this(out param, optParam: optParam)
    {
    }
    public TestClass(out global::System.String param) : this(out param, optParam: default(global::System.Int32))
    {
    }
  }
}