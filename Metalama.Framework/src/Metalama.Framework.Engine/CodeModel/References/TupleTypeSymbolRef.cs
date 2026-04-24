// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class TupleTypeSymbolRef : SymbolRef<ITupleType>
{
    internal ImmutableArray<string> ElementNames { get; }

    public TupleTypeSymbolRef(
        INamedTypeSymbol symbol,
        ImmutableArray<string> elementNames,
        GenericContext? genericContextForSymbolMapping,
        RefFactory refFactory ) : base(
        symbol,
        genericContextForSymbolMapping,
        refFactory )
    {
        this.ElementNames = elementNames;
    }

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
    {
        var translatedSymbol = compilation.CompilationContext.SymbolTranslator.Translate( this.Symbol, this.CompilationContext.Compilation );

        if ( translatedSymbol == null )
        {
            return ReturnNullOrThrow( MetalamaStringFormatter.Instance.Format( this.Symbol ), throwIfMissing, compilation );
        }

        return ConvertDeclarationOrThrow(
            compilation.Factory.GetTupleTypeFromSymbol( (INamedTypeSymbol) translatedSymbol, this.ElementNames, this.SelectGenericContext( genericContext ) )
                .AssertNotNull(),
            compilation,
            interfaceType );
    }
}