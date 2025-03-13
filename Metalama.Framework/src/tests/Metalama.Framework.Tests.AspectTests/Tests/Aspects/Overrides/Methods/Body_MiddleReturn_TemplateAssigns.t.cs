internal class TargetClass
{
  [Override]
  public void VoidMethod(int x)
  {
    global::System.Console.WriteLine("Begin override.");
    Console.WriteLine("Begin target.");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    Console.WriteLine("End target.");
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
    Console.WriteLine("Begin target.");
    if (x == 0)
    {
      result = 42;
      goto __aspect_return_1;
    }
    Console.WriteLine("End target.");
    result = x;
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
    Console.WriteLine("Begin target.");
    if (x?.Equals(default) ?? false)
    {
      result = x;
      goto __aspect_return_1;
    }
    Console.WriteLine("End target.");
    result = x;
    goto __aspect_return_1;
    __aspect_return_1:
      global::System.Console.WriteLine("End override.");
    return (T? )result;
  }
}