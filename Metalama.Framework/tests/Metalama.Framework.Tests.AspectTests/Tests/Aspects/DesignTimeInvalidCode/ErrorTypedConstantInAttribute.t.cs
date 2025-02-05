// Error CS0182 on `int.Parse("42")`: `An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type`
// Error CS0182 on `int.Parse("42")`: `An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type`
[Aspect(int.Parse("42"))]
internal partial class TargetCode1
{
}
[Aspect(PropertyValue = int.Parse("42"))]
internal partial class TargetCode2
{
}