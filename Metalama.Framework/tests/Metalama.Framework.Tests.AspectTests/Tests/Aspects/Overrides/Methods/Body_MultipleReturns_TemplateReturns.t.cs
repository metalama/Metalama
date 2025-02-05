internal class TargetClass
{
  [Override]
  public void VoidMethod(int x)
  {
    global::System.Console.WriteLine("Override.");
    while (x > 0)
    {
      if (x == 42)
      {
        goto __aspect_return_1;
      }
      x--;
    }
    if (x > 0)
    {
      goto __aspect_return_1;
    }
    __aspect_return_1:
      return;
  }
  [Override]
  public int Method(int x)
  {
    global::System.Console.WriteLine("Override.");
    while (x > 0)
    {
      if (x == 42)
      {
        return 42;
      }
      x--;
    }
    if (x > 0)
    {
      return -1;
    }
    return 0;
  }
  [Override]
  public T? GenericMethod<T>(T? x)
  {
    global::System.Console.WriteLine("Override.");
    var z = 42;
    {
      while (z > 0)
      {
        if (z == 42)
        {
          return x;
        }
        z--;
      }
      if (z > 0)
      {
        return x;
      }
      return default;
    }
  }
}