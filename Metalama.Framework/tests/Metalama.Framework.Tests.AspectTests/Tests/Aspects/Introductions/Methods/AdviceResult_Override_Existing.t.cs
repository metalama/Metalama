[TestAspect]
public class TargetClass
{
  public int Method()
  {
    global::System.Console.WriteLine("Aspect code.");
    Console.WriteLine("Original code.");
    return 42;
  }
}