[TestAspect]
public class TargetClass
{
  public static int operator +(TargetClass x, int y)
  {
    global::System.Console.WriteLine("Aspect code.");
    Console.WriteLine("Original code.");
    return y;
  }
}