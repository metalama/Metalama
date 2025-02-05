[TestAspect]
public class TargetClass : BaseClass
{
  public new global::System.Int32 this[global::System.Int32 index]
  {
    get
    {
      global::System.Console.WriteLine("Aspect code.");
      return base[index];
    }
    set
    {
      global::System.Console.WriteLine("Aspect code.");
      base[index] = value;
    }
  }
}