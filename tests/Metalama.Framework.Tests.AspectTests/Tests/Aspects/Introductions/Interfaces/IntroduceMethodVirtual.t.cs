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
  class TestImpl : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest
  {
    public TestImpl()
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
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual.TargetType.TestImpl();
    }
  }
}