// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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