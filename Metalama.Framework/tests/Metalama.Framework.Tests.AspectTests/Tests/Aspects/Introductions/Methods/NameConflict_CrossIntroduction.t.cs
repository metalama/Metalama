[Introduction1]
[Introduction2]
internal class TargetClass
{
  public global::System.Int32 Bar1()
  {
    Foo1_1();
    return (global::System.Int32)Foo1();
    void Foo1_1()
    {
    }
  }
  public global::System.Int32 Bar2()
  {
    Foo2_1();
    return (global::System.Int32)Foo2();
    void Foo2_1()
    {
    }
  }
  public global::System.Int32 Foo1()
  {
    return (global::System.Int32)42;
  }
  public global::System.Int32 Foo2()
  {
    return (global::System.Int32)42;
  }
  public global::System.Int32 Quz()
  {
    Foo1_1();
    return (global::System.Int32)Foo1();
    void Foo1_1()
    {
    }
  }
}