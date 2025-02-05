internal class TargetCode
{
  [Aspect]
  private void Method(int x, int y)
  {
    global::System.Console.WriteLine($"called template a={0} b={x} c={y} d={1} e=2");
    return;
  }
}