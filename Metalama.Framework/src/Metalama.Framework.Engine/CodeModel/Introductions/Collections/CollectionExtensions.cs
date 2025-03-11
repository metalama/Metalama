// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal static class CollectionExtensions
{
    public static ImmutableArray<ParameterBuilderData> ToImmutable( this IParameterBuilderList parameters, IFullRef<IDeclaration> containingDeclaration )
    {
        if ( parameters.Count == 0 )
        {
            return ImmutableArray<ParameterBuilderData>.Empty;
        }
        else
        {
            return parameters.SelectAsImmutableArray<IParameterBuilder, ParameterBuilderData>(
                t => new ParameterBuilderData( (BaseParameterBuilder) t, containingDeclaration ) );
        }
    }
}