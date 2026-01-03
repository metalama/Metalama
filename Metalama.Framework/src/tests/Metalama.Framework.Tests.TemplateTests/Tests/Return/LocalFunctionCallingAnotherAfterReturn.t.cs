private object? Method()
{
  return (global::System.Object? )Outer(this.Method());
  object? Outer(object? input)
  {
    global::System.Console.WriteLine("Outer called");
    return (global::System.Object? )Inner(input);
  }
  object? Inner(object? input_1)
  {
    global::System.Console.WriteLine("Inner called");
    return (global::System.Object? )input_1;
  }
}
