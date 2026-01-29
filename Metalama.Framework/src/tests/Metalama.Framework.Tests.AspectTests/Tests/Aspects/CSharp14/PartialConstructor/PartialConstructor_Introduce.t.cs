// Final Compilation.Emit failed.
// Error CS9275 on `ClassWithImplicit`: `Partial member 'ClassWithImplicit.ClassWithImplicit()' must have an implementation part.`
// Error CS9275 on `ClassWithOtherSignature`: `Partial member 'ClassWithOtherSignature.ClassWithOtherSignature()' must have an implementation part.`
[TheAspect]
internal partial class ClassWithImplicit
{
  public partial ClassWithImplicit();
}
[TheAspect]
internal partial class ClassWithOtherSignature
{
  public ClassWithOtherSignature(int x)
  {
  }
  public partial ClassWithOtherSignature();
}