public class TargetClass : BaseClass
{
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke this.Method
    this.Method();
    // Invoke base.Method
    base.Method();
    // Invoke base.Method
    base.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
}