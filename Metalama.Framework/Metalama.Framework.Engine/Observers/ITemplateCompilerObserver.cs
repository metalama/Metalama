// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Observers
{
    /// <summary>
    /// An interface that can be injected into the service provider to get callbacks from the <see cref="TemplateCompiler"/>
    /// class. For testing only.
    /// </summary>
    public interface ITemplateCompilerObserver : IProjectService
    {
        /// <summary>
        /// Method invoked by the <see cref="TemplateCompiler.TryAnnotate"/> method.
        /// </summary>
        void OnAnnotatedSyntaxNode( SyntaxNode sourceSyntaxRoot, SyntaxNode annotatedSyntaxRoot );
    }
}