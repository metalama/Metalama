[Introduction]
internal partial class TestClass
{
  public TestClass(int param, int optional1 = 42, int optional2 = 42)
  {
  }
  public TestClass(int param)
  {
  }
  public void Foo()
  {
    _ = new TestClass(42);
    _ = new TestClass(42, 42, 42);
    _ = new TestClass(42, optional1: 42);
    _ = new TestClass(42, optional2: 42);
    _ = new TestClass(42, optional1: 42, optional2: 42);
  }
}