[IntroduceAspect]
public class TestClass : IExistingInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_ExistingInterface.IIntroducedInterface
{
  public void ExistingMethod()
  {
    Console.WriteLine("Original interface member.");
  }
  public void IntroducedMethod()
  {
    global::System.Console.WriteLine("Introduced interface member.");
  }
}