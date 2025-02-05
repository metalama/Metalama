class Target
{
  public int Foo
  {
    get
    {
      return this.Foo_Override0;
    }
    set
    {
      this.Foo_Override0 = value;
    }
  }
  public int Bar;
  public int Foo_Override0
  {
    get
    {
      // Should invoke the final declaration.
      _ = this.Bar;
      // Should invoke the final declaration.
      _ = this.Bar;
      // Should invoke the final declaration.
      _ = this.Bar;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
}