[IntroductionAttribute]
public class TargetType
{
  interface ITestContravariant<in T>
  {
  }
  interface ITestCovariant<out T>
  {
  }
  class TestUsage
  {
    public void TestContravariance(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Variance.TargetType.ITestContravariant<global::System.Object> p)
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Variance.TargetType.ITestContravariant<global::System.String> t;
      t = p;
    }
    public void TestCovariance(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Variance.TargetType.ITestCovariant<global::System.String> p)
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Variance.TargetType.ITestCovariant<global::System.Object> t;
      t = p;
    }
  }
}