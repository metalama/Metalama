internal class TargetClass
{
  [Override]
  public void VoidMethod(int x)
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Begin target.");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    Console.WriteLine("End target.");
    __aspect_return_1:
      return;
  }
  [Override]
  public int Method(int x)
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Begin target.");
    if (x == 0)
    {
      return 42;
    }
    Console.WriteLine("End target.");
    return x;
  }
  [Override]
  public T? GenericMethod<T>(T? x)
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Begin target.");
    if (x?.Equals(default) ?? false)
    {
      return x;
    }
    Console.WriteLine("End target.");
    return x;
  }
}