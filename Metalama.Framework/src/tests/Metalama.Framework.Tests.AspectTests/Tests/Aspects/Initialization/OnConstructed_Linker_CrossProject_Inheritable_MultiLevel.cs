// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Linker_CrossProject_Inheritable_MultiLevel;

// <target>
public class MiddleClass : BaseClass
{
    public string MiddleProperty { get; set; } = "middle";
}

// <target>
public class DerivedClass : MiddleClass
{
    public string DerivedProperty { get; set; } = "derived";
}
