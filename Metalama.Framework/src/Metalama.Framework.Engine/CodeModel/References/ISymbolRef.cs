// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

internal interface ISymbolRef : IFullRef
{
    ISymbol Symbol { get; }

    [PublicAPI]
    RefTargetKind TargetKind { get; }
    
    bool SymbolMustBeMapped { get; }
}

internal interface ISymbolRef<out T> : ISymbolRef, IFullRef<T>
    where T : class, ICompilationElement;