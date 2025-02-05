private int Method(int a)
{
  object? y = a;
  var x = 0;
  global::System.Console.WriteLine(x);
  if (y == null)
  {
    var x_1 = 1;
    global::System.Console.WriteLine(x_1);
  }
  var x_2 = 2;
  global::System.Console.WriteLine(x_2);
  if (y == null)
  {
    var x_3 = 3;
    global::System.Console.WriteLine(x_3);
  }
  return this.Method(a);
}