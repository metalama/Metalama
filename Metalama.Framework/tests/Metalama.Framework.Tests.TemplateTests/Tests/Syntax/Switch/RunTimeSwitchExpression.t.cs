private int Method(int a)
{
  var x = global::System.DateTime.Now.DayOfWeek switch
  {
    global::System.DayOfWeek.Monday => "Spaghetti",
    global::System.DayOfWeek.Tuesday => "Salad",
    _ => "McDonald"
  };
  object o = new();
  var y = o switch
  {
    global::System.Collections.Generic.IEnumerable<global::System.Object> enumerable when global::System.Linq.Enumerable.Count(enumerable) > 1 => -1,
    global::System.Collections.Generic.IEnumerable<global::System.Object> enumerable2 => global::System.Linq.Enumerable.Count(enumerable2)};
  return this.Method(a);
}