[TestAspect]
public class TargetClass
{
  public global::System.Int32 this[global::System.Int32 index]
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