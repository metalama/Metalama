[FirstAspect]
[SecondAspect]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: FirstAspect First");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second");
  }
  public TargetCode(int x)
  {
    global::System.Console.WriteLine("TargetCode: FirstAspect First");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second");
  }
  private int Method(int a)
  {
    return a;
  }
}