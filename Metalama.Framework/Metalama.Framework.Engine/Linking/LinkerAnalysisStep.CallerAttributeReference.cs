// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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