// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;

namespace Metalama.Framework.Engine.Transformations;

internal sealed class MemberInjectionContext : TransformationContext
{
    public InjectionNameProvider InjectionNameProvider { get; }

    public AspectReferenceSyntaxProvider AspectReferenceSyntaxProvider { get; }

    public MemberInjectionContext(
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnosticSink,
        InjectionNameProvider injectionNameProvider,
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        ITemplateLexicalScopeProvider lexicalScopeProvider,
        SyntaxGenerationContext syntaxGenerationContext,
        CompilationModel compilation ) : base( serviceProvider, diagnosticSink, syntaxGenerationContext, compilation, lexicalScopeProvider )
    {
        this.AspectReferenceSyntaxProvider = aspectReferenceSyntaxProvider;
        this.InjectionNameProvider = injectionNameProvider;
    }
}