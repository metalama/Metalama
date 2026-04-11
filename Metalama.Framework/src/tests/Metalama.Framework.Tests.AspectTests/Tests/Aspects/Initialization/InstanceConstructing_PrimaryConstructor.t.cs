[Aspect]
public class TargetCode
{
  public int X { get; }
  public TargetCode(int x)
  {
    X = x;
    Console.WriteLine("TargetCode: Aspect");
  }
}