// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.TypeParameterBuilder_StructConstraintAfterNullable_Error;

/*
 * The nullable form of a type parameter of a builder is created and assigned to the return type, and the type parameter is then constrained to
 * value types. The nullable form of a type parameter constrained to value types is Nullable<T>, so the constraint is refused.
 */

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(M),
            buildMethod: methodBuilder =>
            {
                var typeParameter = methodBuilder.AddTypeParameter( "T" );
                methodBuilder.ReturnType = typeParameter.ToNullable();
                typeParameter.TypeKindConstraint = TypeKindConstraint.Struct;
            } );
    }

    [Template]
    private dynamic? M() => null;
}

// <target>
[Aspect]
internal class TargetCode { }
