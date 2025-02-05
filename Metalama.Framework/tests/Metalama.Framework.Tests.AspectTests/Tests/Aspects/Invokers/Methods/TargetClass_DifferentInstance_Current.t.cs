public class TargetClass
{
  public void Method()
  {
    global::System.Console.WriteLine();
  }
  [InvokerAspect]
  public void Invoker(TargetClass instance)
  {
    instance.Method();
    return;
  }
}