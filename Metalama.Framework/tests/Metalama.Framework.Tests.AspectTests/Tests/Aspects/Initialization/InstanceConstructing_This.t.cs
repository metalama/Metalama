[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine($"TargetCode {this}: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}