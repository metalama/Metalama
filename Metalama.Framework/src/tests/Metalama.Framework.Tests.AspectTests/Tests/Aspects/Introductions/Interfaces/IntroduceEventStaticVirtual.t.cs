[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static virtual event global::System.EventHandler TestEvent
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
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventStaticVirtual.TargetType.ITest
  {
    public static event global::System.EventHandler TestEvent
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
  class TestUsage<T>
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventStaticVirtual.TargetType.ITest
  {
    public static void TestUsageMethod()
    {
      T.TestEvent += (global::System.EventHandler)((s, ea) => global::System.Console.WriteLine("Handler"));
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventStaticVirtual.TargetType.TestImplementation.TestEvent += (global::System.EventHandler)((s_1, ea_1) => global::System.Console.WriteLine("Handler"));
    }
  }
}