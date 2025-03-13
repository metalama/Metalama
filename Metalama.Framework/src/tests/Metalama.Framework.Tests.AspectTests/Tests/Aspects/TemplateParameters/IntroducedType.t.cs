[Aspect]
public class Target
{
  private void FromBaseCompilation()
  {
    global::System.Console.WriteLine("Target.IntroducedType");
  }
  private void FromMutableCompilation()
  {
    global::System.Console.WriteLine("Target.IntroducedType");
  }
  class IntroducedType
  {
  }
}