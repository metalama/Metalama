private int Method(int a)
{
  var i = 1;
  switch (i)
  {
    case 0:
      global::System.Console.WriteLine("0");
      break;
    case 1:
      var x = this.Method(a);
      break;
  }
  return this.Method(a);
}