public class TargetClass
{
  public void Method()
  {
  }
  public void Method(int x)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = new global::System.Action(this.Method);
    return;
  }
}
