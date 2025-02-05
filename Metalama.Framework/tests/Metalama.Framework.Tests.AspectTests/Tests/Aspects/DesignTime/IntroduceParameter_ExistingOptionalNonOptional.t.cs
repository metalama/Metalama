[Introduction]
internal partial class TestClass
{
  public TestClass(int param)
  {
  }
  public TestClass(int param, int optParam = 42)
  {
  }
  public void Foo()
  {
    _ = new TestClass(42);
    _ = new TestClass(param: 42);
    _ = new TestClass(42, 42);
    _ = new TestClass(42, optParam: 42);
    _ = new TestClass(optParam: 42, param: 13);
  }
}