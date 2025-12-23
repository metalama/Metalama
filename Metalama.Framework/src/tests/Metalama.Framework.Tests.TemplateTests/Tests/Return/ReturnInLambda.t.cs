private object? Method()
{
  global::System.Func<global::System.Object?, global::System.Object?> lambda = (object? input) =>
  {
    if (input == null)
    {
      return (global::System.Object)"default";
    }
    return (global::System.Object)input;
  };
  var result = lambda(this.Method());
  return (global::System.Object?)result;
}
