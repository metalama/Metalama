// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that the members a union does accept are introduced into it. A property and an event whose accessors are
// written hold no field, so the language permits them, and so is a method and a nested type.

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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.MembersIntoUnion
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceUnion(
                "Result",
                u =>
                {
                    u.Accessibility = Accessibility.Public;
                    u.AddCase( typeof(int) );
                } );

            var target = builder.With( result.Declaration );

            target.IntroduceMethod( nameof(MethodTemplate) );
            target.IntroduceProperty( nameof(PropertyTemplate) );
            target.IntroduceEvent( nameof(ExplicitEventTemplate), buildEvent: e => e.Name = "ExplicitEvent" );
            target.IntroduceClass( "Nested" );
        }

        [Template]
        public int MethodTemplate() => 42;

        [Template]
        public int PropertyTemplate
        {
            get => 42;
            set { }
        }

        [Template]
        public event System.EventHandler? ExplicitEventTemplate
        {
            add { }
            remove { }
        }
    }

    // <target>
    [Introduction]
    public class TargetType { }
}
