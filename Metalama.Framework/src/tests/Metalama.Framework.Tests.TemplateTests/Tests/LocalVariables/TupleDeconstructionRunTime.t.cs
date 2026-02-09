private int Method(int a, int b)
{
  (var first, var second) = (a, b);
  global::System.Console.WriteLine($"a={first}, b={second}");
  return this.Method(a, b);
}