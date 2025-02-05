// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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