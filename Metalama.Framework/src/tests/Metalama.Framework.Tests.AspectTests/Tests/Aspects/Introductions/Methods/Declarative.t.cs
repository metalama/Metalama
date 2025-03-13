[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroducedMethod_Int()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Int32);
  }
  public global::System.Int32 IntroducedMethod_Param(global::System.Int32 x)
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
  public static global::System.Int32 IntroducedMethod_StaticSignature()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Int32);
  }
  public virtual global::System.Int32 IntroducedMethod_VirtualExplicit()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Int32);
  }
  public void IntroducedMethod_Void()
  {
    global::System.Console.WriteLine("This is introduced method.");
  }
}