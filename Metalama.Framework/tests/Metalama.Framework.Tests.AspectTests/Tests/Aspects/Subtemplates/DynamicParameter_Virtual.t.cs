internal class TargetCode
{
  [Aspect]
  private void Method1(int x, int y)
  {
    global::System.Console.WriteLine($"called template a={0} b={x} c={y} d={1} e=2");
    return;
  }
  [DerivedAspect]
  private void Method2(int y, int x)
  {
    global::System.Console.WriteLine($"derived template a={0} b={x} c={y} d={1} e=2");
    return;
  }
}