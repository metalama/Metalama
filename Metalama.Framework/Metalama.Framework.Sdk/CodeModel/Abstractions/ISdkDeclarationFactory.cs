// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Abstractions;

internal interface ISdkDeclarationFactory
{
    public bool TryGetDeclaration( ISymbol symbol, [NotNullWhen( true )] out IDeclaration? declaration );
}