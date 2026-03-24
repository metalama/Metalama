public class TargetClass
{
  public void Method(int x)
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    this.Method((global::System.Int32)new object[] { 1 }[0]);
    return;
  }
}
