// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.CodeFixes;

public interface ICodeRefactoringProviderExtension
{
    Task ProvideRefactoringsAsync( ICodeRefactoringContext context, ProjectKey projectKey, SyntaxNode node );
}