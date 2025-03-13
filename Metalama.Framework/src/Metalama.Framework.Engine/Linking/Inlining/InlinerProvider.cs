// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped(#31108)
#endif

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class InlinerProvider
{
    // TODO: Expression body inliners are disabled because substitution generator does not handle them well.
    private readonly Inliner[] _inliners =
    [
        new MethodAssignmentInliner(),
        new MethodLocalDeclarationInliner(),
        new MethodReturnStatementInliner(),
        new MethodCastReturnStatementInliner(),
        new MethodInvocationInliner(),
        new MethodDiscardInliner(),
        new PropertyGetAssignmentInliner(),
        new PropertyGetReturnInliner(),
        new PropertyGetCastReturnInliner(),
        new PropertyGetLocalDeclarationInliner(),

        // new PropertyGetExpressionBodyInliner(),
        new PropertySetValueAssignmentInliner(),

        // new PropertySetExpressionBodyInliner(),
        new EventAddAssignmentInliner(),
        new EventRemoveAssignmentInliner(),
        new ConstructorInliner()
    ];

    public bool TryGetInliner( ResolvedAspectReference aspectReference, SemanticModel semanticModel, out Inliner? inliner )
    {
        // TODO: Optimize.
        inliner = this._inliners
            .SingleOrDefault(
                i => i.IsValidForTargetSymbol( aspectReference.ResolvedSemantic.Symbol )
                     && i.IsValidForContainingSymbol( aspectReference.ResolvedSemantic.Symbol ) && i.CanInline( aspectReference, semanticModel ) );

        return inliner != null;
    }
}