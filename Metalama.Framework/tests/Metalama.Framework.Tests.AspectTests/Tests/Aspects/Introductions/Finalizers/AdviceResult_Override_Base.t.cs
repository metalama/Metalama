[TestAspect]
public class TargetClass : BaseClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("Aspect code.");
    return;
  }
}