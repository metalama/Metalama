public class TargetClass
{
  public void Method()
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