[Introduction]
internal partial class TestClass
{
  public TestClass(int param, int optional = 42)
  {
  }
  public TestClass(int param, string optional = "42")
  {
  }
  public void Foo()
  {
    _ = new TestClass(42, 42);
    _ = new TestClass(42, "42");
    _ = new TestClass(42, optional: 42);
    _ = new TestClass(42, optional: "42");
  }
}