// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_IntroducedMemberId;

// A member introduced into a source file-local type has no symbol of its own, so its serializable identifier takes the
// discriminator from the type that declares it. See issue #662.
//
// The discriminator is the metadata name that the compiler gives the file-local type. It embeds the name of the
// declaring file and a checksum of its path, and both differ between machines, so this test reports the shape of the
// identifier rather than the identifier itself.

public class IntroduceAndReportAttribute : TypeAspect
{
    [Template]
    public int IntroducedMethod( int a )
    {
        return a;
    }

    [Template]
    private static string ReportId( [CompileTime] string id )
    {
        return id;
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var result = builder.IntroduceMethod( nameof(this.IntroducedMethod) );

        builder.IntroduceMethod(
            nameof(ReportId),
            buildMethod: m => m.Name = "GetIntroducedMemberIdShape",
            args: new { id = MaskDiscriminator( result.Declaration.ToSerializableId().Id ) } );
    }

    /// <summary>
    /// Replaces the value of the file-local discriminator with a constant, so that the result does not depend on the
    /// path of the declaring file.
    /// </summary>
    private static string MaskDiscriminator( string id )
    {
        const string marker = ";File=<";

        var index = id.IndexOf( marker, StringComparison.Ordinal );

        if ( index < 0 )
        {
            return id + " (no file discriminator)";
        }

        var endOfDiscriminator = id.IndexOf( "__", index, StringComparison.Ordinal );

        if ( endOfDiscriminator < 0 )
        {
            return id + " (malformed file discriminator)";
        }

        return id.Substring( 0, index ) + ";File=<masked>" + id.Substring( endOfDiscriminator );
    }
}

// <target>
[IntroduceAndReport]
file class FileLocalTarget { }
