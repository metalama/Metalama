public class TargetClass
{
  public void Method(ref int x)
  {
    x++;
  }
  public void Method(string s)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_ExplicitDelegateTypeWithRef.RefDelegate(this.Method);
    return;
  }
}
