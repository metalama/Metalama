private int Method(int a, string b)
{
  global::System.Console.WriteLine("Parameter: a, Type: int");
  global::System.Console.WriteLine("Parameter: b, Type: string");
  var result = this.Method(a, b);
  return (global::System.Int32)result;
}
