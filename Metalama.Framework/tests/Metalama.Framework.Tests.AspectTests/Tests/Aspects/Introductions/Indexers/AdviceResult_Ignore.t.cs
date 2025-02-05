[TestAspect]
public class TargetClass
{
  public int this[int index]
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