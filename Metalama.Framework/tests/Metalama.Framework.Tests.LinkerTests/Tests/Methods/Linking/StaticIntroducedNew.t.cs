class Target : Base
{
  public static void Foo()
  {
    Foo_Override6();
  }
  private static void Bar_Override1_1()
  {
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  private static void Bar_Override1_2()
  {
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
    // Should invoke override 1_1.
    Target.Bar_Override1_1();
    // Should invoke override 1_2.
    Target.Bar_Override1_2();
    // Should invoke the final declaration.
    Target.Bar();
  }
  private static void Bar_Override3_1()
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
  private static void Bar_Override3_2()
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
  private static void Bar_Override5_1()
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
  private static void Bar_Override5_2()
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
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
    // Should invoke empty code.
    global::Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroducedNew.Base.Bar();
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
  public static new void Bar()
  {
    Bar_Override5_2();
  }
}