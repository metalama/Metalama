[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static virtual void TestMethod()
    {
      global::System.Console.WriteLine("Default");
    }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticVirtual.TargetType.ITest
  {
    public static void TestMethod()
    {
      global::System.Console.WriteLine("Implementation");
    }
  }
  class TestUsage<T>
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticVirtual.TargetType.ITest
  {
    public static void TestUsageMethod()
    {
      T.TestMethod();
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticVirtual.TargetType.TestImplementation.TestMethod();
    }
  }
}