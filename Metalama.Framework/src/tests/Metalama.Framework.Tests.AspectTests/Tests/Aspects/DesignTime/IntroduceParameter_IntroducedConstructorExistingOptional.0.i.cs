namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceParameter_IntroducedConstructorExistingOptional
{
  partial class TestClass
  {
    public TestClass(global::System.Int32 x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced1 = 42, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.String introduced2 = "42")
    {
    }
    public TestClass(global::System.Int32 param, global::System.Int32 optParam = 42, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 introduced1 = 42, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.String introduced2 = "42") : this(param, optParam: optParam)
    {
    }
    public TestClass(global::System.Int32 param) : this(param, optParam: default(global::System.Int32))
    {
    }
  }
}