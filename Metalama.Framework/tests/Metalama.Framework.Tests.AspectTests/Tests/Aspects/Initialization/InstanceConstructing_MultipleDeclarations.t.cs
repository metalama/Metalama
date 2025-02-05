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
public partial class TargetCode
{
  public TargetCode(int x)
  {
    global::System.Console.WriteLine("TargetCode: Aspect");
  }
}
public partial class TargetCode
{
}