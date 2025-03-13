// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;

namespace Metalama.Framework.Engine.Transformations;

internal abstract class TransformationContext
{
    public ProjectServiceProvider ServiceProvider { get; }

    public UserDiagnosticSink DiagnosticSink { get; }

    public SyntaxGenerationContext SyntaxGenerationContext { get; }

    public ContextualSyntaxGenerator SyntaxGenerator => this.SyntaxGenerationContext.SyntaxGenerator;

    /// <summary>
    /// Gets the last compilation model of the linker input.
    /// </summary>
    public CompilationModel FinalCompilation { get; }

    public ITemplateLexicalScopeProvider LexicalScopeProvider { get; }

    protected TransformationContext(
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnosticSink,
        SyntaxGenerationContext syntaxGenerationContext,
        CompilationModel compilation,
        ITemplateLexicalScopeProvider lexicalScopeProvider )
    {
        this.ServiceProvider = serviceProvider;
        this.DiagnosticSink = diagnosticSink;
        this.SyntaxGenerationContext = syntaxGenerationContext;
        this.FinalCompilation = compilation;
        this.LexicalScopeProvider = lexicalScopeProvider;
    }
}