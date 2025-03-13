[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect");
  }
  static TargetCode()
  {
  }
  private int Method(int a)
  {
    return a;
  }
}