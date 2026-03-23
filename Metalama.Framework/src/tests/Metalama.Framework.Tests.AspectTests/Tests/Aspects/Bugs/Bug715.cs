// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER && NET7_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug715;

/*
 * Tests that multi-line statements can be added as initializers to a primary constructor.
 * This is a regression test for https://github.com/metalama/Metalama/issues/715.
 * Previously, only simple assignments were supported for primary constructor initializers,
 * and the error message for unsupported multi-line statements was cut off.
 * Now, the primary constructor is removed and replaced with a regular constructor,
 * so any statement is supported.
 */

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer(
            StatementFactory.Parse( "Console.WriteLine( \"initialized\" );" ),
            InitializerKind.BeforeInstanceConstructor );
    }
}

// <target>
[Aspect]
internal class Target( int x )
{
    public int X { get; } = x;
}

#endif
