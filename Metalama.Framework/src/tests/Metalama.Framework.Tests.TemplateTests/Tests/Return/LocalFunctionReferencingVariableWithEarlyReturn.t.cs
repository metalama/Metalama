private object? Method()
{
  var message = "Hello";
  return (global::System.Object?)LocalFunc(this.Method());
  object? LocalFunc(object? input)
  {
    global::System.Console.WriteLine(message);
    return (global::System.Object?)input;
  }
}