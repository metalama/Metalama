[Retry]
internal class Program
{
  private void IntroducedMethod1(global::System.String name)
  {
    IntroducedMethod2("IntroducedMethod1");
  }
  private void IntroducedMethod2(global::System.String name)
  {
    IntroducedMethod1("IntroducedMethod2");
  }
}