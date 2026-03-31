using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.NullableCastSimplification;
// Case 1: (string?)s returned as string? — the cast is redundant and should be simplified.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class ReturnAsNullableAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
// Case 2: (string?)s assigned to object? — the cast should still be simplified even though
// the surrounding conversion context targets object? (Copilot review feedback: use
// GetTypeInfo(node.Type).Type, not GetTypeInfo(node).ConvertedType).
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class AssignToObjectAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
// Case 3: (string?)s passed as argument to method expecting object? — same concern as case 2.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class PassToObjectParamAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal static class Helper
{
  public static object? Consume(object? value) => value;
}
internal class TargetCode
{
  [ReturnAsNullableAspect]
  public static string? ReturnAsNullable(string s)
  {
    return s;
  }
  [AssignToObjectAspect]
  public static object? AssignToObject(string s)
  {
    object? o = s;
    return o;
  }
  [PassToObjectParamAspect]
  public static object? PassToObjectParam(string s)
  {
    return Helper.Consume(s);
  }
}
