// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroducedDeclarative;

#pragma warning disable CS0067, CS0169

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(SerializeAttribute), typeof(IntroduceMembersAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroducedDeclarative;

internal class IntroduceMembersAttribute : TypeAspect
{
    [Introduce]
    private void M2<T>( (int x, int y) p ) { }

    [Introduce]
    private int _field;

    [Introduce]
    private event EventHandler? Event;

    [Introduce]
    private int Property { get; set; }
}

internal class SerializeAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var arrayBuilder = new ArrayBuilder( typeof(string) );

        foreach (var member in meta.Target.Type.Members())
        {
            var serializableId = member.ToSerializableId();
            arrayBuilder.Add( serializableId.Id );
        }

        return arrayBuilder;
    }
}

// <target>
[IntroduceMembers]
internal class C
{
    [Serialize]
    private string[] M() => null!;
}