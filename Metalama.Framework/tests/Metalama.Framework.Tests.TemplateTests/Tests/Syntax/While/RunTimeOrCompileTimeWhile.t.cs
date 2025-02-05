private int Method(int a)
{
  var i = 0;
  while (true)
  {
    i++;
    if (i >= 1)
    {
      break;
    }
  }
  global::System.Console.WriteLine("Test result = " + i);
  return this.Method(a);
}