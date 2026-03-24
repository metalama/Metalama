public class TargetClass
{
  public void Method(int[] values)
  {
  }
  [InvokerAspect]
  public void Invoker()
  {
    this.Method(new int[] { 1, 2, 3 });
    return;
  }
}