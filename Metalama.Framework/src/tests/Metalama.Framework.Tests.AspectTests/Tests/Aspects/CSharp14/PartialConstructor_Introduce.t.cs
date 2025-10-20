// Final Compilation.Emit failed.
// Error CS9276 on `ClassWithImplicit`: `Partial member 'ClassWithImplicit.ClassWithImplicit()' must have a definition part.`
// Error CS9276 on `ClassWithOtherSignature`: `Partial member 'ClassWithOtherSignature.ClassWithOtherSignature()' must have a definition part.`
[TheAspect]
internal partial class ClassWithImplicit
{
    public partial ClassWithImplicit()
    {
    }
}
[TheAspect]
internal partial class ClassWithOtherSignature
{
    public ClassWithOtherSignature( int x )
    {
    }
    public partial ClassWithOtherSignature()
    {
    }
}