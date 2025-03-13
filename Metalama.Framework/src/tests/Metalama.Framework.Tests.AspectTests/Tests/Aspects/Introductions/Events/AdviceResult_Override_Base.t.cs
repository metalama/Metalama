[TestAspect]
public class TargetClass : BaseClass
{
  public override event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Aspect code.");
      base.Event += value;
    }
    remove
    {
      global::System.Console.WriteLine("Aspect code.");
      base.Event -= value;
    }
  }
}