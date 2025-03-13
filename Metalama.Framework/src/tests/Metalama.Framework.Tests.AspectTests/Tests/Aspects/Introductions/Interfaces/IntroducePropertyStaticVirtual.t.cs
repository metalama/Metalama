[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    static virtual global::System.Int32 TestProperty
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
  class TestImplementation : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticVirtual.TargetType.ITest
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
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticVirtual.TargetType.ITest
  {
    public static void TestUsageMethod()
    {
      T.TestProperty = T.TestProperty + 1;
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticVirtual.TargetType.TestImplementation.TestProperty = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyStaticVirtual.TargetType.TestImplementation.TestProperty + 1;
    }
  }
}