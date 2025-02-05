internal class TargetCode
{
  [Aspect]
  private void Method1(int x)
  {
    global::System.Console.WriteLine("normal template");
    switch (x)
    {
      case 0:
      {
        global::System.Console.WriteLine("base called template");
      }
        break;
      case 1:
      {
        global::System.Console.WriteLine("base called template");
      }
        break;
      case 2:
      {
        global::System.Console.WriteLine("base called template");
      }
        break;
    }
    throw new global::System.Exception();
  }
  [DerivedAspect]
  private void Method2(int x)
  {
    global::System.Console.WriteLine("normal template");
    switch (x)
    {
      case 0:
      {
        global::System.Console.WriteLine("derived called template");
      }
        break;
      case 1:
      {
        global::System.Console.WriteLine("derived called template");
      }
        break;
      case 2:
      {
        global::System.Console.WriteLine("derived called template");
      }
        break;
    }
    throw new global::System.Exception();
  }
}