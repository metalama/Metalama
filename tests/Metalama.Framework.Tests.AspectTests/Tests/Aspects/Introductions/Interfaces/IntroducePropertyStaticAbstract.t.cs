// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestProperty.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestProperty.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `get`: `Method, operator, or accessor 'IntroductionAttribute.TestProperty.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
// Warning CS0626 on `set`: `Method, operator, or accessor 'IntroductionAttribute.TestProperty.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.`
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
