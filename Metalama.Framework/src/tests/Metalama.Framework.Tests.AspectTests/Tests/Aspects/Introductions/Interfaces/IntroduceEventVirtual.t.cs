[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    event global::System.EventHandler TestEvent
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual.TargetType.ITest
  {
    public TestImplementation()
    {
    }
    public event global::System.EventHandler TestEvent
    {
      add
      {
        global::System.Console.WriteLine("Implementation");
      }
      remove
      {
        global::System.Console.WriteLine("Implementation");
      }
    }
  }
  class TestUsage
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual.TargetType.ITest TestUsageMethod(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual.TargetType.ITest instance)
    {
      instance.TestEvent += (global::System.EventHandler)((s, ea) => global::System.Console.WriteLine("Handler"));
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual.TargetType.ITest)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual.TargetType.TestImplementation();
    }
  }
}