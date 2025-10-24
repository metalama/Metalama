// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ConstructedTupleType;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach ( var method in builder.Target.Methods )
        {
            var tupleType = TypeFactory.CreateTupleType( method.Parameters );
            var argsField = builder.IntroduceField( GetFieldName( method ), tupleType ).Declaration;
            builder.With( method ).Override( nameof(this.OverrideTemplate), args: new { argsField } );
        }
    }

    private static string GetFieldName( IMethod method ) => "_" + method.Name + "LastArgs";

    [Template]
    private dynamic? OverrideTemplate( IField argsField )
    {
        var argsFieldType = (ITupleType) argsField.Type;
        argsField.Value = argsFieldType.CreateCreateInstanceExpression( meta.Target.Parameters ).Value;

        return meta.Proceed();
    }
}

// <target>
[TheAspect]
public class C
{
    public void M0() { }

    public void M1( int a ) { }

    public void M2( int a1, string a2 ) { }

    public void M9( int a1, string a2, long a3, int a4, string a5, long a6, int a7, string a8, long a9 ) { }
}