private object? Method()
{
  return (global::System.Object? )LocalFunc(this.Method());
  object? LocalFunc(object? input)
  {
    global::System.Console.WriteLine("LocalFunc called");
    return (global::System.Object? )input;
  }
}