public class TargetClass
{
  public void Method(int x)
  {
  }
  public void Method(string s)
  {
  }
  public static void AcceptAction(Action<int> action)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTyped.TargetClass.AcceptAction(new global::System.Action<global::System.Int32>(this.Method));
    return;
  }
}