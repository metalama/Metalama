public class TargetClass
{
  [Test]
  [DisableAspect]
  public DateTime Property1 { get; set; }
  private double _property2;
  [Test]
  public double Property2
  {
    get
    {
      _testDependency.Foo();
      return _property2;
    }
    set
    {
      _testDependency.Foo();
      _property2 = value;
    }
  }
  private IMyInterface _testDependency;
  public TargetClass(IMyInterface? testDependency = null)
  {
    this._testDependency = testDependency ?? throw new System.ArgumentNullException(nameof(testDependency));
  }
}