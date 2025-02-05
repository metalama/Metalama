[ReturnNumbers]
private object Method()
{
  var numbers = new object[]
  {
    42
  };
  return (global::System.Object)(numbers is { } numbers_1 ? global::System.Linq.Enumerable.ToHashSet(numbers_1) : null);
}