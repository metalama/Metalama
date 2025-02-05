public class TargetClass
{
  [InvokerAspect]
  public void Invoker(DifferentClass instance)
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