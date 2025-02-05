[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public void InvokerBefore()
  { // Invoke this.Method
    this.Method();
    // Invoke base.Method
    base.Method();
    // Invoke base.Method
    base.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
  [InvokerAfterAspect]
  public void InvokerAfter()
  { // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    return;
  }
  public new void Method()
  {
    // Invoke base.Method
    base.Method();
    // Invoke base.Method
    base.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke base.Method
    base.Method();
  }
}