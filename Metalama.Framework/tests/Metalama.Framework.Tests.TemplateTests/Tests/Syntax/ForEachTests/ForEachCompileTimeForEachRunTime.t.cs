private int Method(int a, int b)
{
  var array = global::System.Linq.Enumerable.Range(1, 2);
  foreach (var n in array)
  {
    if (a <= n)
    {
      global::System.Console.WriteLine("Oops a <= " + n);
    }
  }
  foreach (var n_1 in array)
  {
    if (b <= n_1)
    {
      global::System.Console.WriteLine("Oops b <= " + n_1);
    }
  }
  var result = this.Method(a, b);
  return (global::System.Int32)result;
}