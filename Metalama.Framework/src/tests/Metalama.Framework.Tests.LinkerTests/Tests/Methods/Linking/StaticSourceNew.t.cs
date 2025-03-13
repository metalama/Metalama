class Target : Base
{
  public static void Foo()
  {
    Foo_Override6();
  }
  public static new void Bar()
  {
    Bar_Override5_2();
  }
  private static void Bar_Source()
  {
    Console.WriteLine("This is original code.");
  }
  static void Bar_Override1_1()
  {
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  static void Bar_Override1_2()
  {
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke override 1_1.
    Target.Bar_Override1_1();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  static void Bar_Override3_1()
  {
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  static void Bar_Override3_2()
  {
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke override 3_1.
    Target.Bar_Override3_1();
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  static void Bar_Override5_1()
  {
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke the final declaration.
    Target.Bar();
    // Should invoke the final declaration.
    Target.Bar();
  }
  static void Bar_Override5_2()
  {
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke override 5_1.
    Target.Bar_Override5_1();
    // Should invoke the final declaration.
    Target.Bar();
    // Should invoke the final declaration.
    Target.Bar();
  }
  public static void Foo_Override0()
  {
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke source code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticSourceNew.Target.Bar_Source();
    // Should invoke the final declaration.
    Target.Bar();
  }
  public static void Foo_Override2()
  {
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  public static void Foo_Override4()
  {
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke override 3_2.
    Target.Bar_Override3_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  public static void Foo_Override6()
  {
    // Should invoke the final declaration.
    Target.Bar();
    // Should invoke the final declaration.
    Target.Bar();
    // Should invoke the final declaration.
    Target.Bar();
    // Should invoke the final declaration.
    Target.Bar();
  }
}