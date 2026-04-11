[Aspect]
public class TargetCode
{
  public TargetCode()
  {
  }
  static TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}