[TestAspect]
public class TargetClass
{
  public global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("Aspect code.");
      return default(global::System.Int32);
    }
    set
    {
      global::System.Console.WriteLine("Aspect code.");
    }
  }
}