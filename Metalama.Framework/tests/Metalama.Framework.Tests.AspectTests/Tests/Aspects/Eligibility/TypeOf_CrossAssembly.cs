// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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