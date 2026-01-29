// Error CS9275 on `C`: `Partial member 'C.C(int)' must have an implementation part.`
internal partial class C
{
  [TheAspect]
  public partial C(int x);
}
