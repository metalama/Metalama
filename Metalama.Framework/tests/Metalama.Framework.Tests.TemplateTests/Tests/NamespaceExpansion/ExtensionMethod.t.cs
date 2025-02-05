private int Method(int a)
{
  var list = new global::System.Collections.Generic.List<global::System.Int32>();
  var max = global::System.Linq.Enumerable.Max(list);
  var take = global::System.Linq.Enumerable.Take(list, 1);
  var take2 = global::System.Linq.Enumerable.Take(list, (int)a);
  return this.Method(a);
}