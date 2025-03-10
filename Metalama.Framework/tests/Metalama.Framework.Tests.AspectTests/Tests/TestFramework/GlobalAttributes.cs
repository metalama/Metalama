// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Reflection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.GlobalAttributes;

internal class TheAspect : TypeAspect
{
    [Introduce]
    public string Product
        => meta.Target.Compilation.Attributes.OfAttributeType( typeof(AssemblyProductAttribute) ).First().ConstructorArguments.First().Value.ToString();
}

// <target>
[TheAspect]
internal class C { }