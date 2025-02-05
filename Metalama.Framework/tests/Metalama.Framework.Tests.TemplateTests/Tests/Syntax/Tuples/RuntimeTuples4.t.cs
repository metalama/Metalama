private int Method(int a)
{
  var a_1 = 1;
  var b = 2;
  var namedItems = (a: a_1, b: b);
  global::System.Console.WriteLine(namedItems.a);
  return this.Method(a);
}