// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.TypeParameterBuilderMethodInstance;

/*
 * Makes a generic instance of a source method of a non-generic type with a type parameter of a builder, and uses its return type.
 */

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var make = builder.Target.Methods.OfName( nameof(TargetCode.Make) ).Single();

        builder.IntroduceMethod(
            nameof(M),
            buildMethod: methodBuilder =>
            {
                var typeParameter = methodBuilder.AddTypeParameter( "T" );
                methodBuilder.ReturnType = make.MakeGenericInstance( [typeParameter] ).ReturnType;
            } );
    }

    [Template]
    private dynamic? M() => null;
}

// <target>
[Aspect]
internal class TargetCode
{
    public static List<T> Make<T>() => new();
}
