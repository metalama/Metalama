// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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