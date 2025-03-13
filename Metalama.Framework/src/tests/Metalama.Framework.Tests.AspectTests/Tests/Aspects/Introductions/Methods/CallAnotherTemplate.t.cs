[Introduction]
internal class TargetClass
{
  public void AnotherMethod()
  {
    global::System.Console.WriteLine("This is another method.");
  }
  public static void AnotherMethod_Static()
  {
    global::System.Console.WriteLine("This is another method.");
  }
  public void IntroducedMethod()
  {
    global::System.Console.WriteLine("This is introduced method.");
    AnotherMethod();
  }
  public static void IntroducedMethod_Static()
  {
    global::System.Console.WriteLine("This is introduced method.");
    AnotherMethod_Static();
  }
}