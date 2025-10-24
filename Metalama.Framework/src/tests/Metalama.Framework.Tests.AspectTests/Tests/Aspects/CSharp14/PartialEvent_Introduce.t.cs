// Final Compilation.Emit failed.
// Error CS9275 on `PartialEvent`: `Partial member 'C.PartialEvent' must have an implementation part.`
[TheAspect]
internal partial class C
{
  public partial event global::System.Action PartialEvent;
}