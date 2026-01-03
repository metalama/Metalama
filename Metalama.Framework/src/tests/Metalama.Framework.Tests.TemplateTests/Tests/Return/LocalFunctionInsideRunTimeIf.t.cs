private object? Method(object? x)
{
  if (x != null)
  {
    return (global::System.Object? )LocalFunc(this.Method(x));
    object? LocalFunc(object? input)
    {
      global::System.Console.WriteLine("LocalFunc called");
      return (global::System.Object? )input;
    }
  }
  return this.Method(x);
}
