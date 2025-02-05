[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    void TestMethod();
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodAbstract_CrossAssembly.TargetType.ITest
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
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodAbstract_CrossAssembly.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodAbstract_CrossAssembly.TargetType.ITest instance)
    {
      instance.TestMethod();
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodAbstract_CrossAssembly.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodAbstract_CrossAssembly.TargetType.TestImplementation();
    }
  }
}
