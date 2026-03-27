public class Target
{
  [TestAspect]
  private static int Add(int a, int b)
  {
    Console.WriteLine("Before");
    return a + b;
  }
}