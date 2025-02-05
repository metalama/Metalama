// Final Compilation.Emit failed.
// Error CS1736 on `new global::System.Int32[] { 42 }`: `Default parameter value for 'p' must be a compile-time constant`
[Aspect]
internal class TargetCode
{
  public TargetCode(global::System.Int32[] p = new global::System.Int32[]
  {
    42
  }
  )
  {
  }
}