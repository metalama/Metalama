[Override]
internal class TargetClass
{
  [MethodOnly]
  [return: ReturnValueOnly]
  public static TargetClass operator +([ParameterOnly] TargetClass right)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.Attributes_Uninlineable.TargetClass.op_UnaryPlus_Source(right);
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.Attributes_Uninlineable.TargetClass.op_UnaryPlus_Source(right);
  }
  private static TargetClass op_UnaryPlus_Source(TargetClass right)
  {
    return right;
  }
  [MethodOnly]
  [return: ReturnValueOnly]
  public static explicit operator int ([ParameterOnly] TargetClass x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    _ = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.Attributes_Uninlineable.TargetClass.op_Explicit_Source(x);
    return global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.Attributes_Uninlineable.TargetClass.op_Explicit_Source(x);
  }
  private static int op_Explicit_Source(TargetClass x)
  {
    return 42;
  }
}