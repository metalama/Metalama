public class TargetClass
{
  public void Method(int x)
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    this.Method((global::System.Int32)1);
    return;
  }
}
