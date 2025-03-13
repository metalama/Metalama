// Final Compilation.Emit failed.
// Error CS0201 on `42`: `Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement`
internal class TargetCode
{
  [Aspect]
  private void M()
  {
    42;
    return;
  }
}