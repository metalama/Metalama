[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
    _ = 1;
  }
  private int Method(int a)
  {
    return a;
  }
}