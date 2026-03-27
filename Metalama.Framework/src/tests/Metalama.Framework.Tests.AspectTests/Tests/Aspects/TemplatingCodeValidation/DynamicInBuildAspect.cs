// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.DynamicInBuildAspect;

// Regression test for #799: using meta.This (dynamic) in BuildAspect should be forbidden.
internal class A : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // meta.This is dynamic and should not be usable in compile-time methods.
        var x = meta.This;
    }
}
