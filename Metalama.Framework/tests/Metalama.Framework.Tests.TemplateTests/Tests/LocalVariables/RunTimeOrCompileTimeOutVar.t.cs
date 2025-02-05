private int Method(int a)
{
  if (a > 0)
  {
    int.TryParse("0", out var i);
    i++;
    global::System.Console.WriteLine($"i={i}");
  }
  return this.Method(a);
}