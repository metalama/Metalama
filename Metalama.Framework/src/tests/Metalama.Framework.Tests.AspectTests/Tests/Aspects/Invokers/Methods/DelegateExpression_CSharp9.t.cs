public class TargetClass
{
  public void Method()
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = new global::System.Action(this.Method);
    return;
  }
}
