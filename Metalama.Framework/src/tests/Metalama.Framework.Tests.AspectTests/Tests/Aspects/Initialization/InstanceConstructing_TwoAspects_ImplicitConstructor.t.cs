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
    global::System.Console.WriteLine("TargetCode: FirstAspect First1");
    global::System.Console.WriteLine("TargetCode: FirstAspect First2");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second1");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second2");
  }
}