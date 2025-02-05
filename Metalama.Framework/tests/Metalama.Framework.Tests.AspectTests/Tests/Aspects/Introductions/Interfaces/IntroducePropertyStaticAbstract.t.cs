[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static abstract global::System.Int32 TestProperty { get; set; }
  }
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticAbstract.TargetType.ITest
  {
    public static global::System.Int32 TestProperty
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
  class TestUsage<T>
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticAbstract.TargetType.ITest
  {
    public static void TestUsageMethod()
    {
      T.TestProperty = T.TestProperty + 1;
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticAbstract.TargetType.TestImplementation.TestProperty = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticAbstract.TargetType.TestImplementation.TestProperty + 1;
    }
  }
}
