public class TargetClass
{
  [Test]
  public DateTime Method1()
  {
    this._testDependency.Foo();
    return default;
  }
  [Test]
  [DisableAspect]
  public double Method2()
  {
    return default;
  }
  private readonly global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_MemberNotSkippedSkipped.IMyInterface _testDependency;
}