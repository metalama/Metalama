// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.ConstructorSet;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The constructors of an introduced record are registered in the code model and emitted by nothing, so a
        // template that reads them at compile time is the only way an aspect test sees them. The advice is what
        // decides the set, which section 4.1 of Metalama.Framework/docs/introducing-records.md tabulates.
        Report( builder, "PositionalClass", builder.IntroduceRecord( "PositionalClass", buildRecord: r => r.AddPositionalParameter( "Value", typeof(int) ) ).Declaration );
        Report( builder, "NonPositionalClass", builder.IntroduceRecord( "NonPositionalClass" ).Declaration );

        Report(
            builder,
            "PositionalStruct",
            builder.IntroduceRecord( "PositionalStruct", RecordKind.Struct, buildRecord: r => r.AddPositionalParameter( "Value", typeof(int) ) ).Declaration );

        Report( builder, "NonPositionalStruct", builder.IntroduceRecord( "NonPositionalStruct", RecordKind.Struct ).Declaration );

        static void Report( IAspectBuilder<INamedType> aspectBuilder, string name, INamedType record )
        {
            var descriptions = new List<string>();

            foreach ( var constructor in record.Constructors )
            {
                var parameterTypes = new List<string>();

                foreach ( var parameter in constructor.Parameters )
                {
                    parameterTypes.Add( parameter.Type.ToDisplayString() );
                }

                descriptions.Add(
                    $"({string.Join( ",", parameterTypes )}) primary={constructor.IsPrimary} implicit={constructor.IsImplicitlyDeclared}" );
            }

            descriptions.Sort( StringComparer.Ordinal );

            aspectBuilder.IntroduceMethod(
                nameof(ReportTemplate),
                buildMethod: m => m.Name = "Report" + name,
                args: new { description = string.Join( ", ", descriptions ) } );
        }
    }

    [Template]
    public void ReportTemplate( [CompileTime] string description )
    {
        Console.WriteLine( description );
    }
}

// <target>
[Introduction]
public class TargetType { }
