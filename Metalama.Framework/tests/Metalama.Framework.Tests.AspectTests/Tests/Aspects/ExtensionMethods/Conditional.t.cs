[ReturnNumbers]
private object Method()
{
  var numbers = new object[]
  {
    42
  };
  switch (global::System.DateTime.Today.DayOfWeek)
  {
    case global::System.DayOfWeek.Monday:
      return (global::System.Object)(numbers is { } numbers_1 ? global::System.Linq.LinqExtensions.ToReadOnlyList(numbers_1) : null);
    case global::System.DayOfWeek.Tuesday:
      return (global::System.Object)(numbers is { } numbers_2 ? global::System.Linq.LinqExtensions.ToReadOnlyList(global::System.Linq.LinqExtensions.ToReadOnlyList(numbers_2)) : null);
    case global::System.DayOfWeek.Wednesday:
      return (global::System.Object)(global::System.Linq.LinqExtensions.ToReadOnlyList(numbers)is { } iReadOnlyList ? global::System.Linq.LinqExtensions.ToReadOnlyList(iReadOnlyList) : null);
    default:
      return (global::System.Object)(numbers is { } numbers_3 ? global::System.Linq.LinqExtensions.ToReadOnlyList(numbers_3)is { } iReadOnlyList_1 ? global::System.Linq.LinqExtensions.ToReadOnlyList(iReadOnlyList_1) : null : null);
  }
}