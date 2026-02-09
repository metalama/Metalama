[InvokerAspect]
public class TargetClass
{
  public void Invoker()
  { // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
  public void Method()
  {
  }
}
