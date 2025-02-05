public class TargetClass
{
  public void Method()
  {
  }
  [InvokerAspect]
  public void Invoker(TargetClass instance)
  { // Invoke instance.Method
    instance.Method();
    // Invoke instance?.Method
    instance?.Method();
    // Invoke instance.Method
    instance.Method();
    // Invoke instance?.Method
    instance?.Method();
    return;
  }
}