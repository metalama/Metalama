private int Method(int a, int b)
{
  var x = new
  {
    A = a,
    B = b,
    Count = 2
  };
  var y = new
  {
    Count = 2
  };
  global::System.Console.WriteLine(x);
  global::System.Console.WriteLine(x.A);
  global::System.Console.WriteLine(x.Count);
  global::System.Console.WriteLine(y.Count);
  var result = this.Method(a, b);
  return (global::System.Int32)result;
}