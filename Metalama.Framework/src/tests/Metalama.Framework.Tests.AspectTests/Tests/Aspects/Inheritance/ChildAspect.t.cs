internal class Targets
{
  [ParentAspect]
  public class BaseTarget
  {
    public virtual void M()
    {
      global::System.Console.WriteLine("From ChildAspect");
      return;
    }
  }
  public class DerivedTarget : BaseTarget
  {
    public override void M()
    {
      global::System.Console.WriteLine("From ChildAspect");
      Console.WriteLine("Hello, world.");
      return;
    }
  }
}