[Aspect]
private void M()
{
  global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range(0, 10), i => (i: i, 0)), x => x.i);
  global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range(0, 10), i_1 => (i: i_1, 1)), x_1 => x_1.i);
  return;
}