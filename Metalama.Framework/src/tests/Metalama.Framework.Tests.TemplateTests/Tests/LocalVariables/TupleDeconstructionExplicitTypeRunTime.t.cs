private int Method(int a, int b)
{
  (int first, int second) = (a, b);
  global::System.Console.WriteLine($"a={first}, b={second}");
  return this.Method(a, b);
}
