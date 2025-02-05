[Aspect]
public partial class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}