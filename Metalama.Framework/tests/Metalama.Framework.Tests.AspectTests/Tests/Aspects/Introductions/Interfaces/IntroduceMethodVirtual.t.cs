[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    void TestMethod()
    {
      global::System.Console.WriteLine("Default");
    }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest
  {
    public TestImplementation()
    {
    }
    public void TestMethod()
    {
      global::System.Console.WriteLine("Implementation");
    }
  }
  class TestUsage
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest instance)
    {
      instance.TestMethod();
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.TestImplementation();
    }
  }
}