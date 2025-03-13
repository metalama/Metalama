[CompileTime]
internal class C
{
  [CompileTime]
  private void M() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}