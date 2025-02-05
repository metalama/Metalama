namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_Ambiguous
{
  partial class TestClass
  {
    public TestClass(global::System.Int32 param, global::System.Int32 optional = 42, global::System.Int32 introduced1 = 42, global::System.String introduced2 = "42") : this(param, optional: optional)
    {
    }
    public TestClass(global::System.Int32 param) : this(param, optional: default(global::System.Int32))
    {
    }
    public TestClass(global::System.Int32 param, global::System.String optional = "42", global::System.Int32 introduced1 = 42, global::System.String introduced2 = "42") : this(param, optional: optional)
    {
    }
  }
}