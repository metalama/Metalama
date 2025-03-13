public class TargetClass : BaseClass
{
  public override void Method()
  {
    this.Method_Source();
  }
  private void Method_Source()
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    // Invoke this.Method
    this.Method();
    // Invoke this.Method_Source
    this.Method_Source();
    // Invoke this.Method_Source
    this.Method_Source();
    // Invoke this.Method
    this.Method();
    return;
  }
}