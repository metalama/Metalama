// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The closed modifier is a C# 15 feature, so the transformed code of this test needs @LanguageVersion(15.0). Roslyn
// 5.11 declares that version, where Roslyn 5.10 reached the feature through the preview version only. The
// metalamaTests.json of the CSharp15 folder requires ROSLYN_5_11_0_OR_GREATER, so this test runs in the latest Roslyn
// variant only.
// NET8_0_OR_GREATER is required because the compiler emits CompilerFeatureRequiredAttribute on the constructor of a
// closed class, and .NET Framework does not declare that attribute.
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ClosedTypes.IntroduceClosedClass
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceClass( "ClosedType", buildType: t => t.IsClosed = true );

            // The closed keyword goes before the partial keyword, because the language requires partial to sit
            // immediately before the type keyword.
            builder.IntroduceClass(
                "ClosedPartialType",
                buildType: t =>
                {
                    t.IsClosed = true;
                    t.IsPartial = true;
                } );

            // The closed keyword replaces the abstract keyword. A closed class is implicitly abstract, and the
            // compiler reports CS9384 for a declaration that says both.
            builder.IntroduceClass(
                "ClosedAbstractType",
                buildType: t =>
                {
                    t.IsAbstract = true;
                    t.IsClosed = true;
                } );
        }
    }

    // <target>
    [Introduction]
    public class TargetType { }
}
