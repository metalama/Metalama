public class TargetClass
{
  public void Method()
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = this.Method;
    return;
  }
}
