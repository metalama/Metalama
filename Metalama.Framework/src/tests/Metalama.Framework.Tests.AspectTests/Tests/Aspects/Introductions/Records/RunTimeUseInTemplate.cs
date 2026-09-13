// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

// Verifies that a template body uses an introduced type at run time and not only at compile time: the type of a
// typeof expression, the member of an enum, the construction of a record, a with expression over it and the
// invocation of a delegate. Every other test of these five kinds reads the introduced type at compile time alone.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedEnum = builder.IntroduceEnum(
            "Level",
            e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "None" );
                e.AddMember( "High", 10 );
            } );

        var record = builder.IntroduceRecord(
            "Snapshot",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        var introducedDelegate = builder.IntroduceDelegate(
            "Handler",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Int32 );
                d.AddParameter( "value", typeof(int) );
            } );

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            args: new
            {
                enumType = introducedEnum.Declaration, recordType = record.Declaration, delegateType = introducedDelegate.Declaration
            } );
    }

    [Template]
    public void MethodTemplate( [CompileTime] INamedType enumType, [CompileTime] INamedType recordType, [CompileTime] INamedType delegateType )
    {
        Console.WriteLine( enumType.ToTypeOfExpression().Value );

        var member = enumType.Fields.OfName( "High" ).Single();
        Console.WriteLine( member.Value );

        var snapshot = recordType.Constructors.Single( c => c.IsPrimary ).Invoke( 1 );
        Console.WriteLine( snapshot );

        // A with expression over an introduced record, which the language writes over the positional property.
        var updated = ExpressionFactory.Parse( $"new {recordType.FullName}( 1 ) with {{ Value = 2 }}" );
        Console.WriteLine( updated.Value );

        var handler = ExpressionFactory.Parse( $"new {delegateType.FullName}( x => x + 1 )" );
        Console.WriteLine( handler.Value );

        var invoked = ExpressionFactory.Parse( $"(new {delegateType.FullName}( x => x + 1 ))( 41 )" );
        Console.WriteLine( invoked.Value );
    }
}

// <target>
[Introduction]
public class TargetType { }
