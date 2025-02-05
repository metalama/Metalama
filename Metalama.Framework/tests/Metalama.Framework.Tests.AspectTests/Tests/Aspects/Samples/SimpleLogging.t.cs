internal class TargetClass
{
  [Log]
  public static int Add(int a, int b)
  {
    Console.WriteLine("TargetClass.Add(int, int) started.");
    try
    {
      int result;
      if (a == 0)
      {
        throw new ArgumentOutOfRangeException(nameof(a));
      }
      result = a + b;
      Console.WriteLine("TargetClass.Add(int, int) succeeded.");
      return result;
    }
    catch (Exception e)
    {
      Console.WriteLine("TargetClass.Add(int, int) failed: " + e.Message);
      throw;
    }
  }
}