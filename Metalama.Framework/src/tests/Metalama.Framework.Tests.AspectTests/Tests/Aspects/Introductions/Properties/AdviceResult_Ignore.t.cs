[TestAspect]
public class TargetClass
{
  public int Property
  {
    get
    {
      Console.WriteLine("Original code.");
      return 42;
    }
    set
    {
      Console.WriteLine("Original code.");
    }
  }
}