using System;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf_CrossAssembly;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
class TypeAttribute : Attribute
{
    public TypeAttribute(string type) { }
}

// <target>
public partial class TargetClass
{
    [TestAspect]
    [Type(nameof(RunTimeClass))]
    [Type(nameof(RunTimeOrCompileTimeClass))]
    [Type(nameof(CompileTimeClass))]
    void M1() { }

    [TestAspect]
    [Type(nameof(RunTimeClass))]
    [Type(nameof(RunTimeOrCompileTimeClass))]
    void M2() { }

    [TestAspect]
    [Type(nameof(RunTimeClass))]
    void M3() { }

    [TestAspect]
    void M4() { }
}