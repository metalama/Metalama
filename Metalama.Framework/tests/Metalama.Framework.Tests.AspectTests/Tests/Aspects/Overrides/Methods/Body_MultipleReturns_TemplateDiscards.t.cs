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
        _ = (global::System.Int32)42;
        goto __aspect_return_1;
      }
      x--;
    }
    if (x > 0)
    {
      _ = (global::System.Int32)(-1);
      goto __aspect_return_1;
    }
    _ = (global::System.Int32)0;
    goto __aspect_return_1;
    __aspect_return_1:
      return default;
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
          _ = (T? )x;
          goto __aspect_return_1;
        }
        z--;
      }
      if (z > 0)
      {
        _ = (T? )x;
        goto __aspect_return_1;
      }
      _ = (T? )default;
      goto __aspect_return_1;
    }
    __aspect_return_1:
      return default;
  }
}