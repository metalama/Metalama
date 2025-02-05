public class TargetClass : BaseClass
{
  public new void Method()
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
}