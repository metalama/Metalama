public class Target
{
  [TestAspect]
  private static int Add(int a, int b)
  {
    if (BoolSource.Value)
    {
      return default;
    }
    else
    {
      int result;
      Console.WriteLine("Thinking...");
      result = a + b;
      return result;
    }
  }
}