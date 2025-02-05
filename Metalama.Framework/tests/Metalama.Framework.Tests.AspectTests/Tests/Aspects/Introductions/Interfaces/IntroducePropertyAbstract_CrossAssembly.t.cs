[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    global::System.Int32 TestProperty { get; set; }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_CrossAssembly.TargetType.ITest
  {
    public TestImplementation()
    {
    }
    public global::System.Int32 TestProperty
    {
      get
      {
        global::System.Console.WriteLine("Implementation");
        return (global::System.Int32)0;
      }
      set
      {
        global::System.Console.WriteLine("Implementation");
      }
    }
  }
  class TestUsage
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_CrossAssembly.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_CrossAssembly.TargetType.ITest instance)
    {
      instance.TestProperty = instance.TestProperty + 1;
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_CrossAssembly.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyAbstract_CrossAssembly.TargetType.TestImplementation();
    }
  }
}
