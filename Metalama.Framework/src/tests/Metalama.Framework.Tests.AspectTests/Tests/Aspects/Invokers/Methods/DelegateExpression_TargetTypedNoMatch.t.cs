public class TargetClass
{
  public void Method(int x)
  {
  }
  public void Method(string s)
  {
  }
  public static void AcceptDelegate(Delegate d)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTypedNoMatch.TargetClass.AcceptDelegate(new global::System.Action<global::System.Int32>(this.Method));
    return;
  }
}
