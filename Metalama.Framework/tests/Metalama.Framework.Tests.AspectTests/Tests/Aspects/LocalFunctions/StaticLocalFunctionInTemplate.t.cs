internal class C
{
  [TestAspect]
  private void Foo()
  {
    static void Local()
    {
    }
    Local();
    return;
  }
}