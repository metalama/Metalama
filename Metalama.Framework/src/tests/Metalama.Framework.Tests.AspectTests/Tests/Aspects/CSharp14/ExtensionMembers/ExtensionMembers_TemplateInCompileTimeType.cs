// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_TemplateInCompileTimeType;

/*
 * A template declared inside an extension block of a compile-time type is not transformed by the compile-time
 * rewriter, so it must be reported instead of being copied to the compile-time compilation. (#1932)
 */

[CompileTime]
internal static class ExtensionTemplates
{
    extension( int value )
    {
        [Template]
        public int Twice => value * 2;
    }
}

// <target>
internal class C { }
