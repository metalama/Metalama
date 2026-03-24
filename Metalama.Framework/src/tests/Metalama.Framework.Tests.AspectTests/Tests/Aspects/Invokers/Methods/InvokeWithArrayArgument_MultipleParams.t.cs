public class TargetClass
{
  public void Method(int x, string y)
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    this.Method((global::System.Int32)1, (global::System.String)"hello");
    return;
  }
}
