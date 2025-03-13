// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private struct CallerAttributeReference
    {
        public IntermediateSymbolSemantic<IMethodSymbol> ContainingSemantic { get; }

        public IMethodSymbol TargetMethod { get; }

        public IMethodSymbol ReferencingOverrideTarget { get; }

        public InvocationExpressionSyntax InvocationExpression { get; }

        public IReadOnlyList<int> ParametersToFix { get; }

        public CallerAttributeReference(
            IntermediateSymbolSemantic<IMethodSymbol> containingSemantic,
            IMethodSymbol referencingOverrideTarget,
            IMethodSymbol targetMethod,
            InvocationExpressionSyntax invocationExpression,
            IReadOnlyList<int> parametersToFix )
        {
            this.ContainingSemantic = containingSemantic;
            this.TargetMethod = targetMethod;
            this.ReferencingOverrideTarget = referencingOverrideTarget;
            this.InvocationExpression = invocationExpression;
            this.ParametersToFix = parametersToFix;
        }
    }
}