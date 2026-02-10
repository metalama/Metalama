public class TargetClass
{
  [TestAspect]
  public void Foo()
  {
    this.Bar<global::System.Int32>(42);
    return;
  }
  public void Bar<T>(T value)
  {
  }
}
