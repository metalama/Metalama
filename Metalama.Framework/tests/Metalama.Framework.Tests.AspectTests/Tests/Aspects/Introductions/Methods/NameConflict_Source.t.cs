[Introduction]
internal class TargetClass
{
  public int Foo()
  {
    return 42;
  }
  public global::System.Int32 Bar()
  {
    Foo_1();
    return (global::System.Int32)Foo();
    void Foo_1()
    {
    }
  }
}