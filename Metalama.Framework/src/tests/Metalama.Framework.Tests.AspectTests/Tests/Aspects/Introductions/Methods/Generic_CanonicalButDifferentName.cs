// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Generic_CanonicalButDifferentName;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(SomeMethod), args: new { T = builder.Target.TypeParameters[0] } );
    }

    [Template]
    private Task<T> SomeMethod<[CompileTime] T>() => Task.FromResult( default(T) );
}

#pragma warning disable CS8619

// <target>
[TheAspect]
internal class TargetClass<T2> { }