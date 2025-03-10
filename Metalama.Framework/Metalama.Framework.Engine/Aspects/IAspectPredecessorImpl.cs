// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects
{
    internal interface IAspectPredecessorImpl : IAspectPredecessor
    {
        FormattableString FormatPredecessor( ICompilation compilation );

        Location? GetDiagnosticLocation( Compilation compilation );

        int TargetDeclarationDepth { get; }

        ImmutableArray<SyntaxTree> PredecessorTreeClosure { get; }
    }
}