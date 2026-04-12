[FirstAspect]
[SecondAspect]
public class TargetCode
{
  public static int Foo = 42;
  static TargetCode()
  {
    Console.WriteLine("TargetCode: FirstAspect First");
    Console.WriteLine("TargetCode: SecondAspect Second");
  }
}