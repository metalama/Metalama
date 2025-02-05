private int Method(int a)
{
  var i = 1;
  switch (i)
  {
    case 0:
      global::System.Console.WriteLine("0");
      break;
    default:
      global::System.Console.WriteLine("Default");
      break;
  }
  return this.Method(a);
}