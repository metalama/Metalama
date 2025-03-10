// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.ImplementGenericInterface;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var nestedType = builder.IntroduceClass( "TestNestedType" );

        ImplementEquatable( nestedType.Declaration );
        ImplementEquatable( nestedType.Declaration.MakeArrayType() );

        void ImplementEquatable( IType valueType )
        {
            var equatable = builder.Target.Compilation.Factory.GetNamedTypeByReflectionType( typeof(IEquatable<>) )
                .MakeGenericInstance( valueType );

            nestedType.ImplementInterface( equatable );
            nestedType.IntroduceMethod( nameof(Equals), args: new { T = valueType } );
        }
    }

    [Template]
    public bool Equals<[CompileTime] T>( T other ) => true;
}

// <target>
[IntroductionAttribute]
public class TargetType { }