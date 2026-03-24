public class TargetClass
{
  public void Method(int x, string y)
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    this.Method((global::System.Int32)new object[] { 1, "hello" }[0], (global::System.String)new object[] { 1, "hello" }[1]);
    return;
  }
}
