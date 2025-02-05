// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Metalama.Framework.Engine.Formatting;

[ExcludeFromCodeCoverage]
public sealed class ClassificationService
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly ClassifyingCompilationContextFactory _classifyingCompilationContextFactory;

    public ClassificationService( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._classifyingCompilationContextFactory = this._serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>();
    }

    public static bool ContainsCompileTimeCode( SyntaxNode syntaxRoot ) => CompileTimeCodeFastDetector.HasCompileTimeCode( syntaxRoot );

    public ClassifiedTextSpanCollection GetClassifiedTextSpans( SemanticModel model, CancellationToken cancellationToken )
        => this.GetClassifiedTextSpans( model, true, cancellationToken );

    internal ClassifiedTextSpanCollection GetClassifiedTextSpans( SemanticModel model, bool polish, CancellationToken cancellationToken )
    {
        var syntaxRoot = model.SyntaxTree.GetRoot();
        var diagnostics = new DiagnosticBag();

        var compilationContext = this._classifyingCompilationContextFactory.GetInstance( model.Compilation );
        var templateCompiler = new TemplateCompiler( this._serviceProvider, compilationContext );

        _ = templateCompiler.TryAnnotate( syntaxRoot, model, diagnostics, cancellationToken, out var annotatedSyntaxRoot, out _ );

        var text = model.SyntaxTree.GetText();
        var classifier = new TextSpanClassifier( text, cancellationToken );
        classifier.Visit( annotatedSyntaxRoot );

        if ( polish )
        {
            classifier.ClassifiedTextSpans.Polish();
        }

        return classifier.ClassifiedTextSpans;
    }

    internal static ClassifiedTextSpanCollection GetClassifiedTextSpansOfAnnotatedSyntaxTree( SyntaxTree syntaxTree, CancellationToken cancellationToken )
    {
        var text = syntaxTree.GetText();
        var classifier = new TextSpanClassifier( text, cancellationToken );
        classifier.Visit( syntaxTree.GetRoot() );

        return classifier.ClassifiedTextSpans;
    }
}