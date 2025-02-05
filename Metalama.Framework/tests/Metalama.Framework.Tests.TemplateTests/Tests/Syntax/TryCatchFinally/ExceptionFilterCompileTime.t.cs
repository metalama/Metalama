private int Method()
{
  var b = 0;
  try
  {
    // comment
    global::System.Console.WriteLine(0);
    var x = 100 / 1;
    var y = x / 0;
  }
  catch (global::System.Exception e)when (e.GetType().Name.Contains("DivideByZero"))
  {
    // comment
    b = 1;
  }
  global::System.Console.WriteLine(b);
  return this.Method();
}