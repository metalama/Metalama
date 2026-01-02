private object? Method()
{
  object? LocalFunc(object? input)
  {
    global::System.Console.WriteLine("LocalFunc called");
    return (global::System.Object?)input;
  }
  return (global::System.Object?)LocalFunc(this.Method());
}
