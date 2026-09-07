// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The closed modifier is a C# 15 feature, so the test compilation needs the preview language version and the opt-in
// that lets Metalama accept it. ALLOW_PREVIEW_LANG_VERSION is the flag of eng/RoslynPreview.props: the engine reads
// ITypeSymbol.IsClosed only when it is set, and the Roslyn 5.0 variant never sets it. The test is gated on that flag
// and not on the Roslyn variant alone, because the latest variant compiled with the flag unset answers false for this
// class by design, so a test gated on the variant would fail in the default build. Replace the flag with
// ROSLYN_5_10_0_OR_GREATER when issue #1936 removes it from the condition of the reader. NET8_0_OR_GREATER is required
// because the compiler emits CompilerFeatureRequiredAttribute on the constructor of a closed class, and .NET
// Framework does not declare that attribute.
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
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
