private int Method(int a, int b)
{
  (var first, var second) = (a, b);
  global::System.Console.WriteLine($"a={(object)first}, b={(object)second}");
  return this.Method(a, b);
}