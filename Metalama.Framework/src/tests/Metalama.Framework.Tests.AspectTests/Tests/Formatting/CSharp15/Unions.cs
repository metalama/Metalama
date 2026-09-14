// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

// Verifies the third acceptance criterion of issue #1942: a compile-time union declaration is classified as
// compile-time at design time, and a run-time one is not. The classifier reads the annotation that the template
// annotator writes, so the two had to be corrected together.

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.CSharp15.Unions
{
    // We need at least an aspect otherwise the template annotator does not run.
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod() => null;
    }

    [CompileTime]
    internal record CompileTimeCircle( double Radius );

    [CompileTime]
    internal record CompileTimeRectangle( double Width, double Height );

    [CompileTime]
    internal union CompileTimeUnion( CompileTimeCircle, CompileTimeRectangle );

    internal record RunTimeCircle( double Radius );

    internal record RunTimeRectangle( double Width, double Height );

    internal union RunTimeUnion( RunTimeCircle, RunTimeRectangle );
}
