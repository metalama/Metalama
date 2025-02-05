private int Method(int a)
{
  var i = 0;
  do
  {
    i++;
    if (i >= 1)
    {
      break;
    }
  }
  while (true);
  global::System.Console.WriteLine("Test result = " + i);
  var result = this.Method(a);
  return (global::System.Int32)result;
}