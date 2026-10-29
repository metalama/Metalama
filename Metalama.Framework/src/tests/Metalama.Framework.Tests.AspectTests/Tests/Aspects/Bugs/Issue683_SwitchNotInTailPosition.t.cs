internal class Target
{
  [LogAspect]
  public void BracedSection(int i)
  {
    global::System.Console.WriteLine("logged");
    switch (i)
    {
      case 0:
      {
        Console.WriteLine("zero");
        goto __aspect_return_1;
      }
      default:
        break;
    }
    __aspect_return_1:
      global::System.Console.WriteLine("after");
    return;
  }
  [LogAspect]
  public void IfElseInSection(int i, bool b)
  {
    global::System.Console.WriteLine("logged");
    switch (i)
    {
      case 0:
        if (b)
        {
          goto __aspect_return_1;
        }
        else
        {
          goto __aspect_return_1;
        }
      default:
        break;
    }
    __aspect_return_1:
      global::System.Console.WriteLine("after");
    return;
  }
  [LogAspect]
  public void NestedSwitch(int i, int j)
  {
    global::System.Console.WriteLine("logged");
    switch (i)
    {
      case 0:
        switch (j)
        {
          case 0:
            goto __aspect_return_1;
          default:
            goto __aspect_return_1;
        }
      default:
        break;
    }
    __aspect_return_1:
      global::System.Console.WriteLine("after");
    return;
  }
  [LogAspect]
  public void DirectlyInSection(int i)
  {
    global::System.Console.WriteLine("logged");
    switch (i)
    {
      case 0:
        Console.WriteLine("zero");
        break;
      default:
        break;
    }
    global::System.Console.WriteLine("after");
    return;
  }
}