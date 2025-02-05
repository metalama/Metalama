[TestAspect]
public class TargetClass
{
  public event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Aspect code.");
    }
    remove
    {
      global::System.Console.WriteLine("Aspect code.");
    }
  }
}