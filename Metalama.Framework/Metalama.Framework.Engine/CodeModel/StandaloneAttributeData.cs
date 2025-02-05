// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel;

internal sealed class StandaloneAttributeData : IAttributeData
{
    public StandaloneAttributeData( IConstructor constructor )
    {
        this.Constructor = constructor;
    }

    public INamedType Type => this.Constructor.DeclaringType;

    public IConstructor Constructor { get; }

    public ImmutableArray<TypedConstant> ConstructorArguments { get; init; } = ImmutableArray<TypedConstant>.Empty;

    public INamedArgumentList NamedArguments { get; } = NamedArgumentList.Empty;
}