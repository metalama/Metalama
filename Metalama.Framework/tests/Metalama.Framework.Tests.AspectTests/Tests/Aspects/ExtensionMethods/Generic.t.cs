[ReturnNumbers]
private object Method()
{
  var numbers = new object[]
  {
    42
  };
  return (global::System.Object)global::System.Linq.Enumerable.Cast<int>(numbers);
}