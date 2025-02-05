internal class TargetCode
{
  [Aspect]
  private void Method1()
  {
    global::System.Console.WriteLine("regular template");
    global::System.Console.WriteLine($"called template i={1} j=2 k={3}");
    return;
  }
  [DerivedAspect]
  private void Method2()
  {
    global::System.Console.WriteLine("regular template");
    global::System.Console.WriteLine($"derived template i={1} j=2 k={3}");
    return;
  }
  [DerivedAspect]
  private Task Method3()
  {
    global::System.Console.WriteLine($"derived template i={4} j=5 k={6}");
    return this.Method3_Source();
  }
  private async Task Method3_Source()
  {
    await Task.Yield();
  }
}