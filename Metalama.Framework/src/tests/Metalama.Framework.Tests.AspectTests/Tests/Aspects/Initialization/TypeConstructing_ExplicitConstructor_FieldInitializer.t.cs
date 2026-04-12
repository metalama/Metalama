[Aspect]
public class TargetCode
{
  public static int Foo = 42;
  static TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
  }
}