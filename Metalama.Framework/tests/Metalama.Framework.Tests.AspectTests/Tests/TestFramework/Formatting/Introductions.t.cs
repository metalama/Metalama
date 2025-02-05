[Introduction]
internal class TargetClass
{
  public int IntroducedMethod_Int()
  {
    Console.WriteLine("This is introduced method.");
    return default(int);
  }
  public int IntroducedMethod_Param(int x)
  {
    Console.WriteLine($"This is introduced method, x = {x}.");
    return default(int);
  }
  public static int IntroducedMethod_StaticSignature()
  {
    Console.WriteLine("This is introduced method.");
    return default(int);
  }
  public virtual int IntroducedMethod_VirtualExplicit()
  {
    Console.WriteLine("This is introduced method.");
    return default(int);
  }
  public void IntroducedMethod_Void()
  {
    Console.WriteLine("This is introduced method.");
  }
}