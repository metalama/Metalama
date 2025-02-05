[Aspect]
public class TargetCode
{
  public static int Foo = 42;
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect first");
    global::System.Console.WriteLine("TargetCode: Aspect second");
  }
}