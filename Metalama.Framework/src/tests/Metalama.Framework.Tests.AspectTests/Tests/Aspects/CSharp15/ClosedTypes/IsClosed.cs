// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The closed modifier is a C# 15 feature, which only the preview language version parses. The
// @LanguageVersion(preview) directive also turns on the preview language features of the pipeline, because
// TestProjectOptions derives that opt-in from the language version of the project under test. The metalamaTests.json
// of the CSharp15 folder requires ROSLYN_5_10_0_OR_GREATER, so this test runs in the latest Roslyn variant only.
// ALLOW_PREVIEW_LANG_VERSION is the flag of eng/RoslynPreview.props: the engine reads ITypeSymbol.IsClosed only when
// that flag is set, so in the default build of the latest variant the aspect reports false for this class by design,
// and a test that did not require the flag would fail there. Drop this requirement when issue #1936 removes the flag
// from the condition of the reader. NET8_0_OR_GREATER is required because the compiler emits
// CompilerFeatureRequiredAttribute on the constructor of a closed class, and .NET Framework does not declare that
// attribute.
// @LanguageVersion(preview)
// @RequiredConstant(ALLOW_PREVIEW_LANG_VERSION)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

#if TESTRUNNER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Stands for the attribute that the compiler emits on a closed class. No target framework declares it yet, and
    /// the compiler reports CS0656 when it cannot find it.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public sealed class IsClosedTypeAttribute : Attribute { }
}
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ClosedTypes.IsClosed
{
    public class ReportClosednessAttribute : TypeAspect
    {
        [Introduce]
        public bool IsClosed => meta.Target.Type.IsClosed;
    }

    // <target>
#if TESTRUNNER
    [ReportClosedness]
    public closed class ClosedTarget
    {
        public sealed class Case : ClosedTarget { }
    }
#endif
}
