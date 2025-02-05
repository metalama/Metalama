public class TargetClass : BaseClass
{
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