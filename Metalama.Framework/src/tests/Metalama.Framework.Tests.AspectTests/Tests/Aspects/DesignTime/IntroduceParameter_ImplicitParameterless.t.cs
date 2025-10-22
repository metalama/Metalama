// Error CS7036 on `TestClass`: `There is no argument given that corresponds to the required parameter 'introduced1' of 'TestClass.TestClass(int, string)'`
[Introduction]
internal partial class TestClass
{
  public void Foo()
  {
    _ = new TestClass();
  }
}