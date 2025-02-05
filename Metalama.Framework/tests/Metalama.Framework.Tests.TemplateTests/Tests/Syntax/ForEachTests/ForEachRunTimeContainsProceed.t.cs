private void Method(int a, int bb)
{
  var array = global::System.Linq.Enumerable.Range(1, 2);
  foreach (var i in array)
  {
    this.Method(a, bb);
    return;
  }
  return;
}