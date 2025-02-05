[FirstAspect]
[SecondAspect]
public class TargetCode
{
  public static int Foo = 42;
  static TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: FirstAspect First");
    global::System.Console.WriteLine("TargetCode: SecondAspect Second");
  }
}