[Aspect]
public class TargetCode
{
  public static int Foo = 42;
  class IntroducedType : global::System.Object
  {
    public static global::System.Int32 Field;
    static IntroducedType()
    {
      global::System.Console.WriteLine("IntroducedType: Aspect");
      Field = (global::System.Int32)42;
    }
  }
}