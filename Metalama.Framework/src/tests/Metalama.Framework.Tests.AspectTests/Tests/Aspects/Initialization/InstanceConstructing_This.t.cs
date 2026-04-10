[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine($"TargetCode {this}: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}