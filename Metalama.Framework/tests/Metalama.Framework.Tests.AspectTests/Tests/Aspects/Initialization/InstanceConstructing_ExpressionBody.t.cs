[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect");
    _ = 1;
  }
  private int Method(int a)
  {
    return a;
  }
}