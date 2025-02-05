private int Method(int a)
{
  var i = 0;
  while (i < 1)
  {
    i++;
  }
  global::System.Console.WriteLine("Test result = " + i);
  var result = this.Method(a);
  return (global::System.Int32)result;
}