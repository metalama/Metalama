// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.TwoTypes;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @namespace = builder.With( builder.Target.Compilation.GlobalNamespace ).WithChildNamespace( "Implementation" );
        var @class1 = @namespace.IntroduceClass( "TestClass1" );
        var @class2 = @namespace.IntroduceClass( "TestClass2" );

        builder.IntroduceField( "Field1", @class1.Declaration );
        builder.IntroduceField( "Field2", @class2.Declaration );
    }
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType { }