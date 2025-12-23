private object? Method()
{
  object? LocalFunc(object? input)
  {
    if (input == null)
    {
      return (global::System.Object?)"default";
    }
    return (global::System.Object?)input;
  }
  var result = LocalFunc(this.Method());
  return (global::System.Object?)result;
}
