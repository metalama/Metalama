private int Method(int a)
{
  var o = new object ();
  global::System.Console.WriteLine(o.GetType().ToString());
  return this.Method(a);
}