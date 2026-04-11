[Aspect]
public class TargetCode
{
  static TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect first");
  }
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect second");
  }
}