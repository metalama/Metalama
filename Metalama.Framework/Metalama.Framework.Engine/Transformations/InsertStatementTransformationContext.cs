// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Context for code transformation's syntax node evaluation.
/// </summary>
internal abstract class InsertStatementTransformationContext : TransformationContext
{
    protected InsertStatementTransformationContext(
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnosticSink,
        SyntaxGenerationContext syntaxGenerationContext,
        CompilationModel compilation,
        ITemplateLexicalScopeProvider lexicalScopeProvider ) : base(
        serviceProvider,
        diagnosticSink,
        syntaxGenerationContext,
        compilation,
        lexicalScopeProvider ) { }

    public abstract string GetReturnValueVariableName();
}