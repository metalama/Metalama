public class TargetClass
{
  public void Method(int x)
  {
  }
  public void Method(string s)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_ExplicitDelegateType.MyDelegate(this.Method);
    return;
  }
}
