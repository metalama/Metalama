[InvokerAspect]
public class TargetClass
{
  public void Invoker()
  { // Invoke this.Method_Empty
    this.Method_Empty();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
  public void Method()
  {
  }
  private void Method_Empty()
  {
  }
}