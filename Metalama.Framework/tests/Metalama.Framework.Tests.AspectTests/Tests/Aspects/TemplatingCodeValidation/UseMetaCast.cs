// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseMetaCast;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

internal class TestAttribute : TypeAspect
{
    [Template]
    public dynamic? MyTemplate()
    {
        var x = meta.Proceed();

        return meta.Cast( meta.Target.Type, x );
    }
}