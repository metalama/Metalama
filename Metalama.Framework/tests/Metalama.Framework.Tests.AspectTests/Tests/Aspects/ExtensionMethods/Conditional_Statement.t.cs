[ReturnNumbers]
private object Method()
{
  var numbers = new object[]
  {
    42
  };
  if (numbers is { } numbers_1)
    if (global::System.Linq.LinqExtensions.ToReadOnlyList(numbers_1)is { } iReadOnlyList)
      global::System.Linq.LinqExtensions.ToReadOnlyList(iReadOnlyList);
  if (global::System.Linq.LinqExtensions.ToReadOnlyList(numbers)is { } iReadOnlyList_1)
    global::System.Linq.LinqExtensions.ToReadOnlyList(iReadOnlyList_1);
  if (numbers is { } numbers_2)
    global::System.Linq.LinqExtensions.ToReadOnlyList(global::System.Linq.LinqExtensions.ToReadOnlyList(numbers_2));
  return null;
}