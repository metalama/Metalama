class Target
{
  public int Foo
  {
    get
    {
      return this.Foo_Override2;
    }
    set
    {
      this.Foo_Override2 = value;
    }
  }
  public int Foo_Override0
  {
    get
    {
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke empty code.
      this.Bar_Empty = value;
      // Should invoke empty code.
      this.Bar_Empty = value;
      // Should invoke empty code.
      this.Bar_Empty = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  public int Foo_Override1
  {
    get
    {
      // Should invoke empty code.
      _ = this.Bar_Empty;
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
      // Should invoke empty code.
      this.Bar_Empty = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  public int Foo_Override2
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
  private int Bar_Empty
  {
    get => default(int);
    set
    {
    }
  }
  public int Bar;
}