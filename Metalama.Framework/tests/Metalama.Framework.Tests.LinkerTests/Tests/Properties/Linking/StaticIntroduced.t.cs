class Target
{
  public static int Foo
  {
    get
    {
      return Foo_Override6;
    }
    set
    {
      Foo_Override6 = value;
    }
  }
  private static int Bar_Override1_1
  {
    get
    {
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  private static int Bar_Override1_2
  {
    get
    {
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke override 1_1.
      _ = Target.Bar_Override1_1;
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke override 1_1.
      Target.Bar_Override1_1 = value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  private static int Bar_Override3_1
  {
    get
    {
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  private static int Bar_Override3_2
  {
    get
    {
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke override 3_1.
      _ = Target.Bar_Override3_1;
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke override 3_1.
      Target.Bar_Override3_1 = value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  private static int Bar_Override5_1
  {
    get
    {
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  private static int Bar_Override5_2
  {
    get
    {
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke override 5_1.
      _ = Target.Bar_Override5_1;
      // Should invoke the final declaration.
      _ = Target.Bar;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke override 5_1.
      Target.Bar_Override5_1 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  public static int Foo_Override0
  {
    get
    {
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke empty code.
      _ = global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke empty code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticIntroduced.Target.Bar_Empty = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  public static int Foo_Override2
  {
    get
    {
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke override 1_2.
      _ = Target.Bar_Override1_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  public static int Foo_Override4
  {
    get
    {
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke override 3_2.
      _ = Target.Bar_Override3_2;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  public static int Foo_Override6
  {
    get
    {
      // Should invoke the final declaration.
      _ = Target.Bar;
      // Should invoke the final declaration.
      _ = Target.Bar;
      // Should invoke the final declaration.
      _ = Target.Bar;
      // Should invoke the final declaration.
      _ = Target.Bar;
      return 42;
    }
    set
    {
      // Should invoke the final declaration.
      Target.Bar = value;
      // Should invoke the final declaration.
      Target.Bar = value;
      // Should invoke the final declaration.
      Target.Bar = value;
      // Should invoke the final declaration.
      Target.Bar = value;
    }
  }
  public static int Bar
  {
    get
    {
      return Bar_Override5_2;
    }
    set
    {
      Bar_Override5_2 = value;
    }
  }
  private static int Bar_Empty
  {
    get => default(int);
    set
    {
    }
  }
}