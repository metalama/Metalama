// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The closed modifier is a C# 15 feature, so the test compilation needs the preview language version and the opt-in
// that lets Metalama accept it. ALLOW_PREVIEW_LANG_VERSION is the flag of eng/RoslynPreview.props: the engine reads
// ITypeSymbol.IsClosed only when it is set, so the test is skipped otherwise, and it is never set in the Roslyn 5.0
// variant. NET8_0_OR_GREATER is required because the compiler emits CompilerFeatureRequiredAttribute on the
// constructor of a closed class, and .NET Framework does not declare that attribute.
// @LanguageVersion(preview)
// @AllowPreviewLanguageFeatures
// @RequiredConstant(ALLOW_PREVIEW_LANG_VERSION)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

#if TESTRUNNER
namespace System.Runtime.CompilerServices
{
    // The compiler emits this attribute on a closed class and reports CS0656 when it cannot find it. The target
    // framework of this test does not declare it yet, so the test declares it.
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

    [ReportClosedness]
    public abstract class AbstractTarget { }
}
