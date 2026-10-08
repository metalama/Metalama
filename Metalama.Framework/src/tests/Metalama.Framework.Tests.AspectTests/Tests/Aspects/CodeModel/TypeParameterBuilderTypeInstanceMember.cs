// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.TypeParameterBuilderTypeInstanceMember;

/*
 * Constructs List<T> with a type parameter of a builder, and gets the method Add of this type with ForTypeInstance. The definition of the
 * constructed type is List<T>, so the parameter of Add has the type of the type parameter of the builder.
 */

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var listDefinition = (INamedType) typeof(List<>).AsIType();
        var add = listDefinition.Methods.OfName( "Add" ).Single();

        builder.IntroduceMethod(
            nameof(M),
            buildMethod: methodBuilder =>
            {
                var typeParameter = methodBuilder.AddTypeParameter( "T" );
                var listOfT = listDefinition.MakeGenericInstance( [typeParameter] );
                methodBuilder.AddParameter( "item", add.ForTypeInstance( listOfT ).Parameters[0].Type );
            } );
    }

    [Template]
    private void M() { }
}

// <target>
[Aspect]
internal class TargetCode { }
