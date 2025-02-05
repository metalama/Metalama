[TestAspect]
public class TargetClass
{
  public event EventHandler Event
  {
    add
    {
      Console.WriteLine("Original code.");
    }
    remove
    {
      Console.WriteLine("Original code.");
    }
  }
}