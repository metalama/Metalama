[FirstAspect]
[SecondAspect]
public class TargetCode
{
  private int Method(int a)
  {
    return a;
  }
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: FirstAspect First");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second");
  }
}