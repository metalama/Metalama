[MyAspect]
internal class C
{
  public void InSourceCode()
  {
    global::System.Console.WriteLine("Overridden in Layer Second");
    global::System.Console.WriteLine("Overridden in Layer First");
    return;
  }
  public void IntroducedInFirstLayer()
  {
    global::System.Console.WriteLine("Overridden in Layer Second");
    return;
  }
  public void IntroducedInSecondLayer()
  {
  }
}