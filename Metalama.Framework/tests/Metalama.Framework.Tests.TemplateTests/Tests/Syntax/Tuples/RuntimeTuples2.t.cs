private int Method(int a)
{
  var t = (1, 2, 3);
  global::System.Console.WriteLine(t.Item3);
  return this.Method(a);
}