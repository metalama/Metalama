[TestAspect]
public class TargetClass
{
  public static int operator +(TargetClass x, int y)
  {
    Console.WriteLine("Original code.");
    return y;
  }
}