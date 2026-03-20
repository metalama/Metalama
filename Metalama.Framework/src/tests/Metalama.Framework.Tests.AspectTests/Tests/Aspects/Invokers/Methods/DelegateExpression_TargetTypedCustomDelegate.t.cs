public class TargetClass
{
  public void Method(int x)
  {
  }
  public void Method(string s)
  {
  }
  public static void AcceptDelegate(MyIntAction action)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTypedCustomDelegate.TargetClass.AcceptDelegate(this.Method);
    return;
  }
}
