[Introduction1]
[Introduction2]
[Override1]
[Override2]
[Override3]
[Override4]
internal class TargetClass
{
  private void Bar_Introduction2()
  {
    global::System.Console.WriteLine("This is introduced method.");
    this.Bar_Empty();
    this.Bar_Empty();
  }
  public void Bar()
  {
    global::System.Console.WriteLine("This is overridden (3) method.");
    this.Bar_Introduction2();
    this.Bar_Introduction2();
  }
  private void Bar_Empty()
  {
  }
  private void Foo_Introduction1()
  {
    global::System.Console.WriteLine("This is introduced method.");
    this.Foo_Empty();
    this.Foo_Empty();
  }
  private void Foo_Override1()
  {
    global::System.Console.WriteLine("This is overridden (1) method.");
    this.Foo_Introduction1();
    this.Foo_Introduction1();
  }
  private void Foo_Override2()
  {
    global::System.Console.WriteLine("This is overridden (2) method.");
    this.Foo_Override1();
    this.Foo_Override1();
  }
  public void Foo()
  {
    global::System.Console.WriteLine("This is overridden (4) method.");
    this.Foo_Override2();
    this.Foo_Override2();
  }
  private void Foo_Empty()
  {
  }
}