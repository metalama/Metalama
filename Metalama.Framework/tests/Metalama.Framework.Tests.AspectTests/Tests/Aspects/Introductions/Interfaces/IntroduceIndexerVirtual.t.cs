[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    global::System.Int32 this[global::System.Int32 index]
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
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerVirtual.TargetType.ITest
  {
    public TestImplementation()
    {
    }
    public global::System.Int32 this[global::System.Int32 index]
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
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerVirtual.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerVirtual.TargetType.ITest instance)
    {
      instance[42] = instance[42] + 1;
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerVirtual.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerVirtual.TargetType.TestImplementation();
    }
  }
}
