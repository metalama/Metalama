class Target : Base
{
  public void Foo()
  {
    this.Foo_Override6();
  }
  private void Bar_Override1_1()
  {
    // Should invoke base declaration.
    base.Bar();
    // Should invoke base declaration.
    base.Bar();
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  private void Bar_Override1_2()
  {
    // Should invoke base declaration.
    base.Bar();
    // Should invoke override 1_1.
    this.Bar_Override1_1();
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  private void Bar_Override3_1()
  {
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  private void Bar_Override3_2()
  {
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke override 3_1.
    this.Bar_Override3_1();
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  private void Bar_Override5_1()
  {
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke the final declaration.
    this.Bar();
    // Should invoke the final declaration.
    this.Bar();
  }
  private void Bar_Override5_2()
  {
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke override 5_1.
    this.Bar_Override5_1();
    // Should invoke the final declaration.
    this.Bar();
    // Should invoke the final declaration.
    this.Bar();
  }
  public void Foo_Override0()
  {
    // Should invoke base declaration.
    base.Bar();
    // Should invoke base declaration.
    base.Bar();
    // Should invoke base declaration.
    base.Bar();
    // Should invoke the final declaration.
    this.Bar();
  }
  public void Foo_Override2()
  {
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke override 1_2.
    this.Bar_Override1_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  public void Foo_Override4()
  {
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke override 3_2.
    this.Bar_Override3_2();
    // Should invoke the final declaration.
    this.Bar();
  }
  public void Foo_Override6()
  {
    // Should invoke the final declaration.
    this.Bar();
    // Should invoke the final declaration.
    this.Bar();
    // Should invoke the final declaration.
    this.Bar();
    // Should invoke the final declaration.
    this.Bar();
  }
  public new void Bar()
  {
    this.Bar_Override5_2();
  }
}