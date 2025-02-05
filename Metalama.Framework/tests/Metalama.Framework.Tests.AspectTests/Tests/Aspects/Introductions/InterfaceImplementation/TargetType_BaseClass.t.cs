[IntroduceAspect]
public class TargetClass : BaseClass, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_BaseClass.IInterface
{
  public override void ExistingMethod()
  {
    Console.WriteLine("Original interface member");
  }
  public void IntroducedMethod()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
}