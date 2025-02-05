using System;
using Metalama.Framework.Aspects;

namespace CustomSourceGeneratorAttribute;

partial class Target
{
    [MyAspect]
    [SourceGenerator]
    static partial void M();
}

[AttributeUsage(AttributeTargets.Method)]
class SourceGeneratorAttribute : Attribute;

class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}