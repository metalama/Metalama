private object? Method()
{
  return (global::System.Object? )Transform(Format(this.Method()));
  object? Transform(object? input)
  {
    global::System.Console.WriteLine("Transform called");
    return (global::System.Object? )input;
  }
  object? Format(object? input_1)
  {
    global::System.Console.WriteLine("Format called");
    return (global::System.Object? )input_1;
  }
}
