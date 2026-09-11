// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
// @RequiredConstant(ALLOW_PREVIEW_LANG_VERSION)
#endif

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

/*
 * C# 15 lets a collection expression carry a with-element, which passes arguments to the constructor of the
 * collection type. This test places such an element in a run-time expression of a template, so the template compiler
 * has to rebuild the element in the generated code. The element carries the equality comparer of the set, so the
 * count written by the target method is one and not two. See issue #1948.
 *
 * The test requires ALLOW_PREVIEW_LANG_VERSION, the opt-in of eng/RoslynPreview.props, because the consumed Roslyn
 * still marks the with-element as experimental. Without the opt-in the meta syntax rewriter generator removes the
 * declaration, no visitor is emitted for the node, the Roslyn base rewriter runs, and it throws an invalid cast that
 * the pipeline reports as an unexpected exception. Drop the condition when issue #1936 brings a Roslyn that publishes
 * the declaration without the marker.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.CollectionExpressionArguments_RunTime;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        HashSet<string> set = [with( StringComparer.OrdinalIgnoreCase ), "a", "A"];
        Console.WriteLine( set.Count );

        return meta.Proceed();
    }
}

// <target>
internal class TargetType
{
    [TheAspect]
    public void Method() { }
}
