internal class TargetCode
{
  [Aspect]
  private async Task Method1(bool condition)
  {
    global::System.Console.WriteLine("normal template");
    if (condition)
    {
      global::System.Console.WriteLine("virtual method");
      await this.Method1_Source(condition);
      return;
    }
    else
    {
      global::System.Console.WriteLine("virtual method");
      await this.Method1_Source(condition);
      return;
    }
    throw new global::System.Exception();
  }
  private async Task Method1_Source(bool condition)
  {
    await Task.Yield();
  }
  [DerivedAspect]
  private async Task Method2(bool condition)
  {
    global::System.Console.WriteLine("normal template");
    if (condition)
    {
      global::System.Console.WriteLine("overridden virtual method");
      await this.Method2_Source(condition);
      return;
    }
    else
    {
      global::System.Console.WriteLine("overridden virtual method");
      await this.Method2_Source(condition);
      return;
    }
    throw new global::System.Exception();
  }
  private async Task Method2_Source(bool condition)
  {
    await Task.Yield();
  }
}