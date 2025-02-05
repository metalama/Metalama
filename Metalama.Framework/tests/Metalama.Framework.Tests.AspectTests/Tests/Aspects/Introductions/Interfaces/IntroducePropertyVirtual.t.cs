[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    global::System.Int32 TestProperty
    {
      get
      {
        global::System.Console.WriteLine("Default");
        return (global::System.Int32)0;
      }
      set
      {
        global::System.Console.WriteLine("Default");
      }
    }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual.TargetType.ITest
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
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual.TargetType.ITest instance)
    {
      instance.TestProperty = instance.TestProperty + 1;
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual.TargetType.TestImplementation();
    }
  }
}