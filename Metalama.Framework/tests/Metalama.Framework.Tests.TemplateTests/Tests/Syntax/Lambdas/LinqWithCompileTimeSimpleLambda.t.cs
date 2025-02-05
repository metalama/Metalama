private int Method(int a, int b)
{
  var list = new global::System.Collections.Generic.List<global::System.Int32>();
  list.Add(1);
  list.Add(2);
  list.Add(5);
  var p = global::System.Linq.Enumerable.Count(global::System.Linq.Enumerable.Where(list, a_1 => a_1 > 2));
  global::System.Console.WriteLine(p);
  return this.Method(a, b);
}