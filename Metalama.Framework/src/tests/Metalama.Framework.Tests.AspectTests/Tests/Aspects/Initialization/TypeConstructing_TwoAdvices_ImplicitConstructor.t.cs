[Aspect]
public class TargetCode
{
  public static int Foo = 42;
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect first");
    Console.WriteLine("TargetCode: Aspect second");
  }
}