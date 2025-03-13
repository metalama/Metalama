private int Method(int a)
{
  var o = new object ();
  switch (o)
  {
    case global::System.Collections.Generic.IEnumerable<global::System.Object> a_1 when global::System.Linq.Enumerable.Any(a_1):
      global::System.Console.WriteLine("0");
      break;
    default:
      global::System.Console.WriteLine("Default");
      break;
  }
  return this.Method(a);
}