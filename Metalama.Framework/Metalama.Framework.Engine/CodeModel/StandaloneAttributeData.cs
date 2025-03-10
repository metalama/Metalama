// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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