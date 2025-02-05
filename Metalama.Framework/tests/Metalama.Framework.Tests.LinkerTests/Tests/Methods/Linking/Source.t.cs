class Target
{
  public void Foo()
  {
    this.Foo_A3_Override8();
  }
  private void Foo_Source()
  {
    System.Console.WriteLine("This is original code (discarded).");
  }
  public void Bar()
  {
    this.Bar_A4_Override10();
  }
  public void Bar_A1_Override1()
  {
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A2_Override2()
  {
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A2_Override4()
  {
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A3_Override5()
  {
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A3_Override7()
  {
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo_A3_Override6.
    this.Foo_A3_Override6();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A3_Override9()
  {
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Bar_A4_Override10()
  {
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Foo_A2_Override3()
  {
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke this.Foo_Source.
    this.Foo_Source();
    // Should invoke Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Foo_A3_Override6()
  {
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
  public void Foo_A3_Override8()
  {
    // Should invoke this.Foo_A2_Override3.
    this.Foo_A2_Override3();
    // Should invoke this.Foo_A3_Override6.
    this.Foo_A3_Override6();
    // Should invoke this.Foo.
    this.Foo();
    // Should invoke this.Foo.
    this.Foo();
  }
}