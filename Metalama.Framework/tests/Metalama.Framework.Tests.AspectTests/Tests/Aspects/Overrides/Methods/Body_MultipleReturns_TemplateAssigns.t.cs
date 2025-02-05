internal class TargetClass
{
  [Override]
  public void VoidMethod(int x)
  {
    global::System.Console.WriteLine("Begin override.");
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
      object result = null;
    global::System.Console.WriteLine("End override.");
    return;
  }
  [Override]
  public int Method(int x)
  {
    global::System.Console.WriteLine("Begin override.");
    global::System.Int32 result;
    while (x > 0)
    {
      if (x == 42)
      {
        result = 42;
        goto __aspect_return_1;
      }
      x--;
    }
    if (x > 0)
    {
      result = -1;
      goto __aspect_return_1;
    }
    result = 0;
    goto __aspect_return_1;
    __aspect_return_1:
      global::System.Console.WriteLine("End override.");
    return (global::System.Int32)result;
  }
  [Override]
  public T? GenericMethod<T>(T? x)
  {
    global::System.Console.WriteLine("Begin override.");
    T? result;
    var z = 42;
    {
      while (z > 0)
      {
        if (z == 42)
        {
          result = x;
          goto __aspect_return_1;
        }
        z--;
      }
      if (z > 0)
      {
        result = x;
        goto __aspect_return_1;
      }
      result = default;
      goto __aspect_return_1;
    }
    __aspect_return_1:
      global::System.Console.WriteLine("End override.");
    return (T? )result;
  }
}