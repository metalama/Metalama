// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a top-level introduced union reaches the editor as a union. A top-level introduced type is
// re-created by the design-time generator through CreatePartialType, and Roslyn reports a union as a struct, so
// without the guard the editor would receive a partial struct against a union declaration, which is CS0261. That is
// issue #1943.

#if TESTRUNNER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Stands for the interface that the compiler requires a union to implement. No target framework declares it
    /// yet, and the compiler reports CS0518 when it cannot find it.
    /// </summary>
    public interface IUnion
    {
        object Value { get; }
    }

    /// <summary>
    /// Stands for the attribute that the compiler emits on a union. No target framework declares it yet, and the
    /// compiler reports CS0656 when it cannot find its constructor.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false )]
    public sealed class UnionAttribute : Attribute { }
}
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceUnion_IntoNamespace
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.ContainingNamespace )
                .IntroduceUnion(
                    "TopLevelResult",
                    u =>
                    {
                        u.Accessibility = Accessibility.Public;
                        u.AddCase( typeof(int) );
                        u.AddCase( typeof(string) );
                    } );
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [Introduction]
        public class TargetType { }
    }
}
