// Final Compilation.Emit failed.
// Error CS8795  on `NonVoidMethod`: `Partial method 'TargetClass.NonVoidMethod()' must have an implementation part because it has accessibility modifiers.`
// Error CS8795  on `PublicMethod`: `Partial method 'TargetClass.PublicMethod()' must have an implementation part because it has accessibility modifiers.`
[Introduction]
internal partial class TargetClass
{
  private partial global::System.Int32 NonVoidMethod();
  public partial void PublicMethod();
}