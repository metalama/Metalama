// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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