class Target
{
  public int Foo
  {
    get
    {
      return this.Foo_Override6;
    }
    set
    {
      this.Foo_Override6 = value;
    }
  }
  private int Bar_Override1_1
  {
    get
    {
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
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
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  private int Bar_Override1_2
  {
    get
    {
      // Should invoke empty code.
      _ = this.Bar_Empty;
      // Should invoke override 1_1.
      _ = this.Bar_Override1_1;
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke empty code.
      this.Bar_Empty = value;
      // Should invoke override 1_1.
      this.Bar_Override1_1 = value;
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  private int Bar_Override3_1
  {
    get
    {
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  private int Bar_Override3_2
  {
    get
    {
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke override 3_1.
      _ = this.Bar_Override3_1;
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke override 3_1.
      this.Bar_Override3_1 = value;
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  private int Bar_Override5_1
  {
    get
    {
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  private int Bar_Override5_2
  {
    get
    {
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke override 5_1.
      _ = this.Bar_Override5_1;
      // Should invoke the final declaration.
      _ = this.Bar;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke override 5_1.
      this.Bar_Override5_1 = value;
      // Should invoke the final declaration.
      this.Bar = value;
      // Should invoke the final declaration.
      this.Bar = value;
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
  public int Foo_Override2
  {
    get
    {
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = this.Bar_Override1_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      this.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  public int Foo_Override4
  {
    get
    {
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = this.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = this.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      this.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      this.Bar = value;
    }
  }
  public int Foo_Override6
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
  public int Bar
  {
    get
    {
      return this.Bar_Override5_2;
    }
    set
    {
      this.Bar_Override5_2 = value;
    }
  }
  private int Bar_Empty
  {
    get => default(int);
    set
    {
    }
  }
}