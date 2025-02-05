private int Method(int a)
{
  global::System.Console.WriteLine(1);
  global::System.Console.WriteLine(2);
  global::System.Console.WriteLine(3);
  global::System.Console.WriteLine(4);
  global::System.Console.WriteLine(5);
  global::System.Console.WriteLine(6);
  global::System.Console.WriteLine("Test result = 6");
  var result = this.Method(a);
  return (global::System.Int32)result;
}