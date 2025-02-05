internal class TargetClass
{
  [Override]
  public static int operator -(TargetClass x)
  {
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes.TargetClass.op_UnaryNegation_Source(x);
    global::System.Console.WriteLine("This is the overridden method.");
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes.TargetClass.op_UnaryNegation_Source(x);
  }
  private static int op_UnaryNegation_Source(TargetClass x)
  {
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
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes.TargetClass.op_Implicit_Source(x);
    global::System.Console.WriteLine("This is the overridden method.");
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.CallerAttributes.TargetClass.op_Implicit_Source(x);
  }
  private static int op_Implicit_Source(TargetClass x)
  {
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