// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Visitors;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

internal partial class SymbolGenericContext
{
    private sealed class TypeSymbolMapper : TypeSymbolRewriter
    {
        public SymbolGenericContext GenericContext { get; }

        public TypeSymbolMapper( SymbolGenericContext genericContext ) : base( genericContext._compilationContext.AssertNotNull().Compilation )
        {
            this.GenericContext = genericContext;
        }

        internal override ITypeSymbol Visit( ITypeParameterSymbol typeSymbolParameter )
        {
            return this.GenericContext.MapToSymbol( typeSymbolParameter );
        }
    }
}