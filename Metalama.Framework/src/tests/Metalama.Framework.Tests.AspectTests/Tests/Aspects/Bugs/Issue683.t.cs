internal class Target
{
  [LogAspect]
  public void Method(int i)
  {
    global::System.Console.WriteLine("logged");
    if (i == 0)
    {
      return;
    }
    Console.WriteLine("method");
    return;
  }
  [LogAspect]
  public void MethodWithExplicitReturn(int i)
  {
    global::System.Console.WriteLine("logged");
    if (i == 0)
    {
      return;
    }
    Console.WriteLine("method");
    return;
  }
}