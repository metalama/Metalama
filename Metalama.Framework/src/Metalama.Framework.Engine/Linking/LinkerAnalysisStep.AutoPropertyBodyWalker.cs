// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

// ReSharper disable MissingIndent
// ReSharper disable BadExpressionBracesIndent

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private sealed class AutoPropertyBodyWalker : CSharpSyntaxWalker
    {
        private List<FieldExpressionSyntax>? _fieldExpressions;

        public IReadOnlyList<FieldExpressionSyntax> FieldExpressions => this._fieldExpressions ??= [];
        
        public override void VisitFieldExpression( FieldExpressionSyntax node )
        {
            (this._fieldExpressions ??= new List<FieldExpressionSyntax>()).Add( node );
            base.VisitFieldExpression( node );
        }
    }
}

#endif