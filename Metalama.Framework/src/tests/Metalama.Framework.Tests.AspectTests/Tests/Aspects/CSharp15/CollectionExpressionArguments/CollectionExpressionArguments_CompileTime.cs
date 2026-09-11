// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
#endif

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

/*
 * The counterpart of CollectionExpressionArguments_RunTime for compile-time scope. The collection expression and
 * its with-element are evaluated when the aspect runs, so the template compiler keeps them verbatim and the
 * compile-time compilation binds them. The set is built with the ordinal-ignore-case comparer, so its count is one,
 * and that value is the literal the target method writes. See issue #1948.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.CollectionExpressionArguments_CompileTime;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var set = meta.CompileTime<HashSet<string>>( [with( StringComparer.OrdinalIgnoreCase ), "a", "A"] );
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
