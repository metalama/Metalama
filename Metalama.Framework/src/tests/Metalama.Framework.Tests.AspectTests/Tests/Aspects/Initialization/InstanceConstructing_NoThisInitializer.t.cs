[Aspect]
public class TargetCode : BaseClass
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  public TargetCode(int x) : base(x)
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  public TargetCode(double x) : this()
  {
  }
  private int Method(int a)
  {
    return a;
  }
}