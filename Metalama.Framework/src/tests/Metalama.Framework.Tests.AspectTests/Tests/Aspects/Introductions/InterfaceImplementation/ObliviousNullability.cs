// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

/*
 * Tests that when an interface member has oblivious nullability (e.g., from a .NET Framework assembly),
 * the [InterfaceMember] template's explicit nullable annotation is preserved in the generated code.
 * Regression test for https://github.com/metalama/Metalama/issues/817.
 */

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IObliviousInterface) );
        }

        [InterfaceMember]
        public event ObliviousHandler? PropertyChanged;

        [InterfaceMember]
        public string? ObliviousProperty { get; set; }

        [InterfaceMember]
        public string? ObliviousMethod( string? x )
        {
            return x;
        }

        [InterfaceMember]
        public int ValueTypeProperty { get; set; }

        [InterfaceMember]
        public int ValueTypeMethod( int x )
        {
            return x;
        }

        [InterfaceMember]
        public int? NullableValueTypeProperty { get; set; }

        [InterfaceMember]
        public int? NullableValueTypeMethod( int? x )
        {
            return x;
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}
