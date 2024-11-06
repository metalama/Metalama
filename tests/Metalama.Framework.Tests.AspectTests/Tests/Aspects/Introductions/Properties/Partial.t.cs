// Final Compilation.Emit failed.
// Error CS9248  on `PartialProperty`: `Partial property 'TargetClass.PartialProperty' must have an implementation part.`
[Introduction]
internal partial class TargetClass
{
  public partial global::System.Int32 PartialProperty { get; set; }
}