[InvokerAspect]
public class TargetClass : BaseClass
{
  public void Invoker()
  { // Invoke base.Method
    base.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
  public override void Method()
  {
  }
}