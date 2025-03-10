// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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