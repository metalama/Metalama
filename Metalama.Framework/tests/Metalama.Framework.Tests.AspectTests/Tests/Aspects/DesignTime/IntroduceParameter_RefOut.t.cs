[Introduction]
internal partial class TestClass
{
  public TestClass(ref int param, int optParam = 42)
  {
  }
  public TestClass(out string param, int optParam = 42)
  {
    param = "42";
  }
  public void Foo()
  {
    var f = 42;
    _ = new TestClass(ref f);
    _ = new TestClass(out var g);
  }
}