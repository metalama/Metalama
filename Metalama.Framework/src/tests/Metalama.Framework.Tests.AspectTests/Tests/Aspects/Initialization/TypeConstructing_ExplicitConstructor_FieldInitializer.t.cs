[Aspect]
public class TargetCode
{
  public static int Foo = 42;
  static TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect");
  }
}