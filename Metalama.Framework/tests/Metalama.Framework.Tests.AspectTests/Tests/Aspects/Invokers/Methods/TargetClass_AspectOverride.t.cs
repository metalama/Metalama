public class TargetClass
{
  [OverrideAspect]
  public void Method()
  { // Invoke this.Method_Source
    this.Method_Source();
    // Invoke this.Method_Source
    this.Method_Source();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method
    this.Method();
    // Invoke this.Method_Source
    this.Method_Source();
  }
  private void Method_Source()
  {
  }
  [InvokerBeforeAspect]
  public void InvokerBefore()
  { // Invoke this.Method
    this.Method();
    // Invoke this.Method_Source
    this.Method_Source();
    // Invoke this.Method_Source
    this.Method_Source();
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
    // Invoke this.Method
    return;
  }
}