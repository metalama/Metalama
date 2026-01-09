private int Method(int a, int b)
{
  (var first, var second) = ("Hello", "World");
  global::System.Console.WriteLine($"{first} {second}");
  return this.Method(a, b);
}