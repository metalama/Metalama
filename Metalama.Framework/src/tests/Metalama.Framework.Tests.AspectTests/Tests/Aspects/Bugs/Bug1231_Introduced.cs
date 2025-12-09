// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1231_Introduced;

public class IntroduceMethodAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Use the target type's own type parameter
        var typeParameter = builder.Target.TypeParameters[0];
        var nullableTypeParameter = typeParameter.ToNullable();
        var listType = ( (INamedType) TypeFactory.GetType( typeof(List<>) ) ).WithTypeArguments( nullableTypeParameter );

        builder.IntroduceMethod(
            nameof(GetValues),
            buildMethod: methodBuilder =>
            {
                methodBuilder.ReturnType = listType;
            } );
    }

    [Template]
    public dynamic? GetValues()
    {
        Console.WriteLine( $"Return type: {meta.Target.Method.ReturnType.ToType()}" );

        return default;
    }
}

// <target>
[IntroduceMethodAspect]
internal class TargetCode<T> { }
