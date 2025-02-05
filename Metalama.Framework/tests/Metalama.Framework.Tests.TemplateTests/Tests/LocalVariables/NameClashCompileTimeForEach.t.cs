private int Method(int a, int b)
{
  string text = "a = " + a;
  global::System.Console.WriteLine(text);
  string text_1 = "b = " + b;
  global::System.Console.WriteLine(text_1);
  return this.Method(a, b);
}