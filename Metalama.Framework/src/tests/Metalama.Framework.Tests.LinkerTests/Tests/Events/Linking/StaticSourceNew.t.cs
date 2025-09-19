internal class Target : Base
{
  public static event EventHandler Foo
  {
    add
    {
      Foo_Override6 += value;
    }
    remove
    {
      Foo_Override6 -= value;
    }
  }
  public static new event EventHandler Bar
  {
    add
    {
      Bar_Override5_2 += value;
    }
    remove
    {
      Bar_Override5_2 -= value;
    }
  }
  private static event EventHandler Bar_Source
  {
    add
    {
      Console.WriteLine("This is original code (discarded).");
    }
    remove
    {
      Console.WriteLine("This is original code (discarded).");
    }
  }
  private static event EventHandler Bar_Override1_1
  {
    add
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  private static event EventHandler Bar_Override1_2
  {
    add
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke override 1_1.
      Target.Bar_Override1_1 += value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke override 1_1.
      Target.Bar_Override1_1 -= value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  private static event EventHandler Bar_Override3_1
  {
    add
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  private static event EventHandler Bar_Override3_2
  {
    add
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke override 3_1.
      Target.Bar_Override3_1 += value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke override 3_1.
      Target.Bar_Override3_1 -= value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  private static event EventHandler Bar_Override5_1
  {
    add
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  private static event EventHandler Bar_Override5_2
  {
    add
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke override 5_1.
      Target.Bar_Override5_1 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke override 5_1.
      Target.Bar_Override5_1 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  public static event EventHandler Foo_Override0
  {
    add
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke source code.
      global::Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.StaticSourceNew.Target.Bar_Source -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  public static event EventHandler Foo_Override2
  {
    add
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke override 1_2.
      Target.Bar_Override1_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  public static event EventHandler Foo_Override4
  {
    add
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke override 3_2.
      Target.Bar_Override3_2 -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
  public static event EventHandler Foo_Override6
  {
    add
    {
      // Should invoke the final declaration.
      Target.Bar += value;
      // Should invoke the final declaration.
      Target.Bar += value;
      // Should invoke the final declaration.
      Target.Bar += value;
      // Should invoke the final declaration.
      Target.Bar += value;
    }
    remove
    {
      // Should invoke the final declaration.
      Target.Bar -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
      // Should invoke the final declaration.
      Target.Bar -= value;
    }
  }
}