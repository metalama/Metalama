public partial class TargetClass : BaseClass
{
  public override void Foo()
  {
    this.Foo_Source();
  }
  private void Foo_Source()
  {
    Console.WriteLine("Override method with no aspect override");
  }
  public sealed override void Bar()
  {
    Console.WriteLine("Sealed override method with no aspect override");
  }
  public new virtual void Baz()
  {
    this.Baz_Source();
  }
  private void Baz_Source()
  {
    Console.WriteLine("Hiding virtual method with no aspect override");
  }
  public virtual void Qux()
  {
    this.Qux_Source();
  }
  private void Qux_Source()
  {
    Console.WriteLine("Virtual method with no aspect override");
  }
  [TestAspect]
  public void Test()
  {
    this.Foo_Source();
    this.Bar();
    this.Baz_Source();
    this.Qux_Source();
    return;
  }
}