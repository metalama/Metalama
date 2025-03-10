// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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