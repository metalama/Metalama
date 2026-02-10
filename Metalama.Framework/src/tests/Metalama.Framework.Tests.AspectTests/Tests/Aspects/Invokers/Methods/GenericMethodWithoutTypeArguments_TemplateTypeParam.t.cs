public class TargetClass
{
  [TestAspect]
  public void Foo()
  {
    this.Bar<global::System.Int32>(new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.GenericMethodWithoutTypeArguments_TemplateTypeParam.TestData<global::System.Int32>());
    return;
  }
  public void Bar<T>(TestData<T> data)
  {
  }
}
