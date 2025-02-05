class Target
{
  public event System.EventHandler Foo
  {
    add
    {
      this.Foo_A3_Override8 += value;
    }
    remove
    {
      this.Foo_A3_Override8 -= value;
    }
  }
  private event System.EventHandler Foo_Source
  {
    add
    {
      System.Console.WriteLine("This is original code (discarded).");
    }
    remove
    {
      System.Console.WriteLine("This is original code (discarded).");
    }
  }
  public event System.EventHandler Bar
  {
    add
    {
      this.Bar_A4_Override10 += value;
    }
    remove
    {
      this.Bar_A4_Override10 -= value;
    }
  }
  public event System.EventHandler Bar_A1_Override1
  {
    add
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A2_Override2
  {
    add
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A2_Override4
  {
    add
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A3_Override5
  {
    add
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A3_Override7
  {
    add
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A3_Override9
  {
    add
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Bar_A4_Override10
  {
    add
    {
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Foo_A2_Override3
  {
    add
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke this.Foo_Source.
      this.Foo_Source += value;
      // Should invoke Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke this.Foo_Source.
      this.Foo_Source -= value;
      // Should invoke Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Foo_A3_Override6
  {
    add
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
  public event System.EventHandler Foo_A3_Override8
  {
    add
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 += value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 += value;
      // Should invoke this.Foo.
      this.Foo += value;
      // Should invoke this.Foo.
      this.Foo += value;
    }
    remove
    {
      // Should invoke this.Foo_A2_Override3.
      this.Foo_A2_Override3 -= value;
      // Should invoke this.Foo_A3_Override6.
      this.Foo_A3_Override6 -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
      // Should invoke this.Foo.
      this.Foo -= value;
    }
  }
}