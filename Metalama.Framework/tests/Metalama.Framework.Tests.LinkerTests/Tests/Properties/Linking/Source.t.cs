class Target
{
  public int Foo
  {
    get
    {
      return this.Foo_A3_Override8;
    }
    set
    {
      this.Foo_A3_Override8 = value;
    }
  }
  private int Foo_Source
  {
    get
    {
      System.Console.WriteLine("This is original code (discarded).");
      return 42;
    }
    set
    {
      System.Console.WriteLine("This is original code (discarded).");
    }
  }
  public int Bar
  {
    get
    {
      return this.Bar_A4_Override10;
    }
    set
    {
      this.Bar_A4_Override10 = value;
    }
  }
  public int Bar_A1_Override1
  {
    get
    {
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A2_Override2
  {
    get
    {
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A2_Override4
  {
    get
    {
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A3_Override5
  {
    get
    {
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A3_Override7
  {
    get
    {
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo_A3_Override6.
      _ = this.Foo_A3_Override6;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A3_Override9
  {
    get
    {
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Bar_A4_Override10
  {
    get
    {
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Foo_A2_Override3
  {
    get
    {
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke this.Foo_Source.
      _ = this.Foo_Source;
      // Should invoke Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke this.Foo_Source.
      this.Foo_Source = value;
      // Should invoke Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Foo_A3_Override6
  {
    get
    {
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
  public int Foo_A3_Override8
  {
    get
    {
      // Should invoke this.Foo_A2_Override3.
      _ = this.Foo_A2_Override3;
      // Should invoke this.Foo_A3_Override6.
      _ = this.Foo_A3_Override6;
      // Should invoke this.Foo.
      _ = this.Foo;
      // Should invoke this.Foo.
      _ = this.Foo;
      return 42;
    }
    set
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 = value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 = value;
      // Should invoke this.Foo.
      this.Foo = value;
      // Should invoke this.Foo.
      this.Foo = value;
    }
  }
}