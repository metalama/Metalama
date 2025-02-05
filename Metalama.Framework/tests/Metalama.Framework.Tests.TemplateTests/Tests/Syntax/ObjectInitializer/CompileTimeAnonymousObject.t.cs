private int Method(int a, int b)
{
  global::System.Console.WriteLine("a");
  var result = this.Method(a, b);
  return (global::System.Int32)result;
}