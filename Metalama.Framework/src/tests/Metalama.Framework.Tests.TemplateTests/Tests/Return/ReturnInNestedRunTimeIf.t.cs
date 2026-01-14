private object? Method(object? a, object? b)
{
  if (a == null)
  {
    if (b == null)
    {
      global::System.Console.WriteLine("TargetCode.Method(object?, object?)/b is null.");
      return null;
    }
    global::System.Console.WriteLine("TargetCode.Method(object?, object?)/a is null.");
  }
  return this.Method(a, b);
}