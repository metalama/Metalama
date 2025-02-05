[TestAspect]
public class TargetClass : BaseClass
{
  public new global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("Aspect code.");
      return base.Property;
    }
    set
    {
      global::System.Console.WriteLine("Aspect code.");
      base.Property = value;
    }
  }
}