public class TargetClass
{
  public void Method()
  {
    global::System.Console.WriteLine();
  }
  private void Method_Source()
  {
  }
  [InvokerAspect]
  public void Invoker(TargetClass instance)
  {
    instance.Method_Source();
    return;
  }
}