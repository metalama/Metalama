public class TargetClass
{
  [Test]
  [DisableAspect]
  public DateTime Method1()
  {
    return default;
  }
  [Test]
  public double Method2()
  {
    this._testDependency.Foo();
    return default;
  }
  private readonly global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_MemberSkippedNotSkipped.IMyInterface _testDependency;
}