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
 * The cases where a with-element meets compile-time code inside a run-time collection expression. Every collection
 * expression below carries the value of a parameter of the target method, which is a run-time expression, so the
 * collection expression stays in run-time scope and the template compiler has to rebuild the with-element in the
 * generated code. The positions exercised are: a compile-time expression as the argument of the with-element, a
 * named argument in the with-element, a spread element beside the with-element, a collection expression that carries
 * a with-element nested in another collection expression, a with-element in a statement that a compile-time loop
 * repeats, and a with-element in a statement that a compile-time condition selects.
 *
 * Two shapes are deliberately absent.
 *
 * A collection expression whose elements are all compile-time is itself compile-time, and the local that receives it
 * is then a compile-time local that leaves no statement behind. That rule belongs to collection expressions in
 * general and not to the with-element, and the compile-time scope is covered by
 * CollectionExpressionArguments_CompileTime.
 *
 * A collection expression that carries a compile-time element beside a run-time one crashes the template compiler,
 * which is issue #1994. That defect belongs to the collection expression of C# 12: it reproduces with no
 * with-element, and a with-element neither causes it nor changes it.
 *
 * The test requires ALLOW_PREVIEW_LANG_VERSION for the reason given in CollectionExpressionArguments_RunTime. See
 * issue #1948.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.CollectionExpressionArguments_Mixed;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var capacity = meta.CompileTime( 2 );
        List<string> byCapacity = [with( capacity ), meta.Target.Parameters[0].Value, "b"];

        HashSet<string> named = [with( comparer: StringComparer.OrdinalIgnoreCase ), meta.Target.Parameters[0].Value];

        HashSet<string> spread = [with( StringComparer.OrdinalIgnoreCase ), ..byCapacity, "A"];

        List<HashSet<string>> nested = [[with( StringComparer.OrdinalIgnoreCase ), meta.Target.Parameters[0].Value]];

        foreach (var word in meta.CompileTime( new[] { "first", "second" } ))
        {
            HashSet<string> repeated = [with( StringComparer.OrdinalIgnoreCase ), meta.Target.Parameters[0].Value];
            Console.WriteLine( word + repeated.Count );
        }

        if (meta.Target.Method.Parameters.Count == 1)
        {
            HashSet<string> conditional = [with( StringComparer.OrdinalIgnoreCase ), meta.Target.Parameters[0].Value];
            Console.WriteLine( conditional.Count );
        }

        Console.WriteLine( byCapacity.Count + named.Count + spread.Count + nested.Count );

        return meta.Proceed();
    }
}

// <target>
internal class TargetType
{
    [TheAspect]
    public void Method( string p ) { }
}
