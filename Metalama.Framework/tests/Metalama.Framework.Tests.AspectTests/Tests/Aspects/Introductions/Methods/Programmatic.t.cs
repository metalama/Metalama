[Introduction]
internal class TargetClass
{
  private global::System.Object IntroducedMethod_Accessibility()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Object);
  }
  public static global::System.Object IntroducedMethod_IsStatic()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Object);
  }
  public virtual global::System.Object IntroducedMethod_IsVirtual()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Object);
  }
  public global::System.Object IntroducedMethod_Parameters(global::System.Int32 x, global::System.Int32 y = 42)
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Object);
  }
  public global::System.Int32 IntroducedMethod_ReturnType()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return default(global::System.Int32);
  }
}