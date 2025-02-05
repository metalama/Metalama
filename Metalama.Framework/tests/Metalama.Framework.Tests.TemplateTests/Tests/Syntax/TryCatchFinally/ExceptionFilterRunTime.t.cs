private int Method(int a)
{
  try
  {
    global::System.Console.WriteLine(1);
    return this.Method(a);
  }
  catch (global::System.Exception e)when (e.GetType().Name.Contains("DivideByZero"))
  {
    return (global::System.Int32)(-1);
  }
}