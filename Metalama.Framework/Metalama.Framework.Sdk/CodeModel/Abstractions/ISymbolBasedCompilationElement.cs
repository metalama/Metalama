// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Abstractions;

internal interface ISymbolBasedCompilationElement : ICompilationElement
{
    ISymbol Symbol { get; }

    IGenericContext GenericContextForSymbolMapping { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Symbol"/> property must be mapped with the <see cref="IGenericContext"/>.
    /// Returns <c>false</c> is the symbol is already mapped.
    /// </summary>
    bool SymbolMustBeMapped { get; }
}