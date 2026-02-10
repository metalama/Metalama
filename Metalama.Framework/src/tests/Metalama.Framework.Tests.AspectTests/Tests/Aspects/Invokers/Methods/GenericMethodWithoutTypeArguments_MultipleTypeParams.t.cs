public class TargetClass
{
  [TestAspect]
  public void Foo()
  {
    this.Bar<global::System.String, global::System.Int32>("hello", 42);
    return;
  }
  public void Bar<TKey, TValue>(TKey key, TValue value)
  {
  }
}
