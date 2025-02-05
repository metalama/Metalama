[Aspect]
public class TargetCode
{
  public int X { get; }
  public TargetCode(int x)
  {
    this.X = x;
    global::System.Console.WriteLine("TargetCode: Aspect");
  }
}