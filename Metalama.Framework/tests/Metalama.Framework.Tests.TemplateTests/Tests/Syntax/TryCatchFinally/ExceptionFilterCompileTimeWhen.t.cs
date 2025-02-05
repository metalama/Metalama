private int Method(int a)
{
  try
  {
    return (global::System.Int32)1;
  }
  catch (global::System.Exception e)when (e.GetType().Name.Contains("DivideByZero"))
  {
    return (global::System.Int32)(-1);
  }
}