[TestAspect]
public class TargetClass
{
  public int this[int index]
  {
    get
    {
      global::System.Console.WriteLine("Aspect code.");
      Console.WriteLine("Original code.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Aspect code.");
      Console.WriteLine("Original code.");
    }
  }
}