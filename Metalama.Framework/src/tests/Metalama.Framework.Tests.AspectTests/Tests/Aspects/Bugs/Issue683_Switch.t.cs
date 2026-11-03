internal class Target
{
  [LogAspect]
  public void BracedSection(int i)
  {
    global::System.Console.WriteLine("logged");
    if (i < 0)
    {
      return;
    }
    switch (i)
    {
      case 0:
      {
        Console.WriteLine("zero");
        return;
      }
      default:
        break;
    }
    return;
  }
  [LogAspect]
  public void IfElseInSection(int i, bool b)
  {
    global::System.Console.WriteLine("logged");
    if (i < 0)
    {
      return;
    }
    switch (i)
    {
      case 0:
        if (b)
        {
          return;
        }
        else
        {
          return;
        }
      default:
        break;
    }
    return;
  }
  [LogAspect]
  public void NestedSwitch(int i, int j)
  {
    global::System.Console.WriteLine("logged");
    if (i < 0)
    {
      return;
    }
    switch (i)
    {
      case 0:
        switch (j)
        {
          case 0:
            return;
          default:
            return;
        }
      default:
        break;
    }
    return;
  }
  [LogAspect]
  public void DirectlyInSection(int i)
  {
    global::System.Console.WriteLine("logged");
    if (i < 0)
    {
      return;
    }
    switch (i)
    {
      case 0:
        Console.WriteLine("zero");
        break;
      default:
        break;
    }
    return;
  }
}