[TestAspect]
public class TargetClass : BaseClass
{
  public new global::System.Int32 Method()
  {
    global::System.Console.WriteLine("Aspect code.");
    return base.Method();
  }
}