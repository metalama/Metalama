private object? Method()
{
  var lambda = (object? input) =>
  {
    if (input == null)
    {
      return (global::System.Object)"default";
    }
    return (global::System.Object)input;
  };
  var result = lambda(this.Method());
  return (global::System.Object? )result;
}