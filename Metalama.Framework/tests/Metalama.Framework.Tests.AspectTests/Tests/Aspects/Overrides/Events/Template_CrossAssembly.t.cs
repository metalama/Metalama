[TestAspect]
internal class TargetClass
{
  public event EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Aspect code");
      Console.WriteLine("Original code");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Aspect code");
      Console.WriteLine("Original code");
      return;
    }
  }
}