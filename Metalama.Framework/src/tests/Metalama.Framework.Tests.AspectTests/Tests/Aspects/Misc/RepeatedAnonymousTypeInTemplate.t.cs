[Aspect]
private void M()
{
  global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range(0, 10), i => new { i = i }), x => x.i);
  global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range(0, 10), i_1 => new { i = i_1 }), x_1 => x_1.i);
  return;
}