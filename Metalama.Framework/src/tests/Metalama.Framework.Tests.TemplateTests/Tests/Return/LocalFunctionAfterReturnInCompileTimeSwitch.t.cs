private object? Method(int x)
{
  return (global::System.Object? )LocalFunc(this.Method(x));
  object? LocalFunc(object? input)
  {
    global::System.Console.WriteLine("LocalFunc called");
    return (global::System.Object? )input;
  }
}
