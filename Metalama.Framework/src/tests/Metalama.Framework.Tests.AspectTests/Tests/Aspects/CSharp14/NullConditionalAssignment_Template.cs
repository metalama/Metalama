// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.NullConditionalAssignment_Template;

internal class TheAspect : TypeAspect
{
    [Introduce]
    public object? Property
    {
        get
        {
            meta.Target.Property.Value?.Property = 1;
            meta.Target.Property.Value?.Property?.Property = 2;

            return null;
        }
    }
}

[TheAspect]
internal class C;

#endif