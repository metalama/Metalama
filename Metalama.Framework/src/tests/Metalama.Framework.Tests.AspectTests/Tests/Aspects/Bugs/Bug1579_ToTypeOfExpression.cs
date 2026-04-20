// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579_ToTypeOfExpression;

internal class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(GetTypes) );
    }

    [Template]
    private object?[] GetTypes()
    {
        var openDefinition = (INamedType) TypeFactory.GetType( typeof(List<>) );
        var canonicalSelfInstance = meta.Target.Type.MakeGenericInstance( meta.Target.Type.TypeParameters.ToArray<IType>() );

        return new object?[]
        {
            // Default behavior: unbound generic type stays unbound.
            openDefinition.ToTypeOfExpression().Value,

            // preferConstructedType=true on a generic instance bound to its own type parameters emits the constructed form.
            canonicalSelfInstance.ToTypeOfExpression( preferConstructedType: true ).Value,

            // Default behavior: the generic instance bound to its own type parameters is emitted as the unbound generic type (backward-compatible).
            canonicalSelfInstance.ToTypeOfExpression().Value
        };
    }
}

// <target>
[TestAspect]
internal partial class Target<T> { }
