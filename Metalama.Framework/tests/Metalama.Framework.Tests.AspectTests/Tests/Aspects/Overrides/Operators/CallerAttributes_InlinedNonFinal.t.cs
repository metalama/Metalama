internal class TargetClass
{
  [Override]
  public static int operator -(TargetClass x)
  {
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass.op_UnaryNegation_Override(x);
    global::System.Console.WriteLine("This is the overridden method.");
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass.op_UnaryNegation_Override(x);
  }
  private static global::System.Int32 op_UnaryNegation_Override(global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    MethodWithCallerMemberName(42, name1: "op_UnaryNegation", name2: "op_UnaryNegation");
    MethodWithCallerMemberName(42, y: 27, name1: "op_UnaryNegation", name2: "op_UnaryNegation");
    MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "op_UnaryNegation");
    MethodWithCallerMemberName(42, "foo", 27, name2: "op_UnaryNegation");
    MethodWithCallerMemberName(42, "foo", 27, "bar");
    return 42;
  }
  [Override]
  public static implicit operator int (TargetClass x)
  {
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass.op_Implicit_Override(x);
    global::System.Console.WriteLine("This is the overridden method.");
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass.op_Implicit_Override(x);
  }
  private static global::System.Int32 op_Implicit_Override(global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes_InlinedNonFinal.TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    MethodWithCallerMemberName(42, name1: "op_Implicit", name2: "op_Implicit");
    MethodWithCallerMemberName(42, y: 27, name1: "op_Implicit", name2: "op_Implicit");
    MethodWithCallerMemberName(42, name1: "foo", y: 27, name2: "op_Implicit");
    MethodWithCallerMemberName(42, "foo", 27, name2: "op_Implicit");
    MethodWithCallerMemberName(42, "foo", 27, "bar");
    return 42;
  }
  public static void MethodWithCallerMemberName(int x, [CallerMemberName] string name1 = "", int y = 0, [CallerMemberName] string name2 = "")
  {
  }
}