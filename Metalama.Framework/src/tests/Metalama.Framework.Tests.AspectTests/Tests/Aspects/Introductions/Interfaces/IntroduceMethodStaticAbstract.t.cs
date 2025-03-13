[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static abstract void TestMethod();
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticAbstract.TargetType.ITest
  {
    public static void TestMethod()
    {
      global::System.Console.WriteLine("Implementation");
    }
  }
  class TestUsage<T>
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticAbstract.TargetType.ITest
  {
    public static void TestUsageMethod()
    {
      T.TestMethod();
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticAbstract.TargetType.TestImplementation.TestMethod();
    }
  }
}