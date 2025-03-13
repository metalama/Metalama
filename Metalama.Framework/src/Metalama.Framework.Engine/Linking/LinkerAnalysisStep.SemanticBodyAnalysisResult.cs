// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private sealed class SemanticBodyAnalysisResult
    {
        /// <summary>
        /// Gets properties of return statements.
        /// </summary>
        public IReadOnlyDictionary<ReturnStatementSyntax, ReturnStatementProperties> ReturnStatements { get; }

        // TODO: There is currently no case where this is necessary, but I'm not sure that is correct.
        // ReSharper disable once MemberCanBePrivate.Local
        // ReSharper disable once UnusedAutoPropertyAccessor.Local

        /// <summary>
        /// Gets a value indicating whether the end-point of the body is reachable. This can be true only for void-returning methods.
        /// </summary>
        public bool HasReachableEndPoint { get; }

        public IReadOnlyList<BlockSyntax> BlocksWithReturnBeforeUsingLocal { get; }

        public SemanticBodyAnalysisResult(
            IReadOnlyDictionary<ReturnStatementSyntax, ReturnStatementProperties> returnStatements,
            bool hasReachableEndPoint,
            IReadOnlyList<BlockSyntax> blocksWithReturnBeforeUsingLocal )
        {
            this.ReturnStatements = returnStatements;
            this.HasReachableEndPoint = hasReachableEndPoint;
            this.BlocksWithReturnBeforeUsingLocal = blocksWithReturnBeforeUsingLocal;
        }
    }
}