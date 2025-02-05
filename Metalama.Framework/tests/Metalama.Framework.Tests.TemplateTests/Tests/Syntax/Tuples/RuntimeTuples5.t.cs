private int Method(int a)
{
  var items = (a: 1, b: 2, 3);
  global::System.Console.WriteLine(items.a);
  return this.Method(a);
}