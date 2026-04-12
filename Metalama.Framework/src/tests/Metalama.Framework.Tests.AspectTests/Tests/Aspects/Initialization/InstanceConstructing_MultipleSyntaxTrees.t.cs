[Aspect]
public partial class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}