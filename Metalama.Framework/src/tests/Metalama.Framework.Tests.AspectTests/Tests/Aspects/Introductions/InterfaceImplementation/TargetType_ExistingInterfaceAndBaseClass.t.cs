[IntroduceAspect]
public class TestClass : BaseClass, IExistingInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_ExistingInterfaceAndBaseClass.IIntroducedInterface
{
  public void ExistingMethod()
  {
    Console.WriteLine("Original interface member.");
  }
  public override void ExistingBaseMethod()
  {
    Console.WriteLine("Original base class member.");
  }
  public void IntroducedMethod()
  {
    global::System.Console.WriteLine("Introduced interface member.");
  }
}