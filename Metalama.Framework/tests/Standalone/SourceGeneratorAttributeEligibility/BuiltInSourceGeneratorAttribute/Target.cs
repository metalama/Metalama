using Metalama.Framework.Aspects;
using System.Text.RegularExpressions;

class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}

partial class Target
{
    [MyAspect]
    [GeneratedRegex("^[^@]+@[^@]+\\.[^@]+$")]
    private static partial Regex SimpleEmailRegex();
}