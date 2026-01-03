private object? Method()
{
  return (global::System.Object? )OuterFunc(this.Method());
  object? OuterFunc(object? input)
  {
    return (global::System.Object? )InnerFunc(input);
    object? InnerFunc(object? value)
    {
      global::System.Console.WriteLine("InnerFunc called");
      return (global::System.Object? )value;
    }
  }
}
