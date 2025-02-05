[TestAspect]
public class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("Aspect code.");
    return;
  }
}