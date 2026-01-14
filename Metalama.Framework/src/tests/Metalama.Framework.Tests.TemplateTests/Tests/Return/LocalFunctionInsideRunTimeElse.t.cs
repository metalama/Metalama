private object? Method(object? x)
{
  if (x != null)
  {
    return this.Method(x);
  }
  else
  {
    return (global::System.Object? )LocalFunc("null input");
    object? LocalFunc(object? input)
    {
      global::System.Console.WriteLine(input);
      return (global::System.Object? )input;
    }
  }
  return null;
}