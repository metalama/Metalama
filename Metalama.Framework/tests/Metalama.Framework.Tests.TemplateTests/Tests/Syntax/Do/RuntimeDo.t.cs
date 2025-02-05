private int Method(int a)
{
  var i = 0;
  do
  {
    i++;
  }
  while (i < 1);
  global::System.Console.WriteLine("Test result = " + i);
  var result = this.Method(a);
  return (global::System.Int32)result;
}