// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Enums.CopiedFromSource;

public enum SourceEnum
{
    Alpha = 5,
    Beta = 7,
    Gamma = 9
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var source = (INamedType) TypeFactory.GetType( typeof(SourceEnum) );

        // The value of a member of another enum is read as a TypedConstant, which the overload of AddMember that
        // takes one accepts directly.
        builder.IntroduceEnum(
            "CopiedEnum",
            buildEnum: e =>
            {
                e.Accessibility = Accessibility.Public;

                foreach ( var member in source.Facets.Enum!.Members )
                {
                    e.AddMember( member.Name, member.ConstantValue!.Value );
                }
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
