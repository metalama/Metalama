// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace Metalama.Framework.Engine.Analyzers;

[DiagnosticAnalyzer( LanguageNames.CSharp )]
[UsedImplicitly]
public class MetalamaPerformanceAnalyzer : DiagnosticAnalyzer
{
    // Range: 830-839
    internal static readonly DiagnosticDescriptor NormalizeWhitespace = new(
        "LAMA0830",
        "NormalizeWhitespace is expensive.",
        "The NormalizeWhitespace method is expensive and should only be called when necessary or when performance doesn't matter. Consider using the conditional NormalizeWhitespaceIfNecessary instead.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    internal static readonly DiagnosticDescriptor SyntaxNodeWith = new(
        "LAMA0831",
        "Avoid chained With calls on SyntaxNodes.",
        "Avoid chained With calls on SyntaxNodes, use the PartialUpdate method instead to avoid allocating garbage nodes.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    // ReSharper disable once MemberCanBePrivate.Global
    internal static readonly DiagnosticDescriptor WithTrivia = new(
        "LAMA0832",
        "Avoid WithLeadingTrivia and WithTrailingTrivia calls.",
        "Avoid WithLeadingTrivia and WithTrailingTrivia calls, use the WithTriviaIfNecessary method instead.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( NormalizeWhitespace, SyntaxNodeWith, WithTrivia );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.ReportDiagnostics );
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction( InitializeCompilation );
    }

    private static void InitializeCompilation( CompilationStartAnalysisContext context )
    {
        context.RegisterOperationAction( AnalyzeNormalizeWhitespace, OperationKind.Invocation, OperationKind.MethodReference );

        var syntaxNodeTypeSymbol = context.Compilation.GetTypeByMetadataName( typeof(SyntaxNode).FullName! )!;

        var syntaxNodePartialUpdateExtensionsSymbol =
            context.Compilation.GetTypeByMetadataName( "Metalama.Framework.Engine.Utilities.Roslyn.SyntaxNodePartialUpdateExtensions" )!;

        context.RegisterOperationAction(
            operationContext => AnalyzeSyntaxNodeWithChains( operationContext, syntaxNodeTypeSymbol, syntaxNodePartialUpdateExtensionsSymbol ),
            OperationKind.Invocation );

        context.RegisterOperationAction( AnalyzeWithTrivia, OperationKind.Invocation, OperationKind.MethodReference );
    }

    private static void AnalyzeNormalizeWhitespace( OperationAnalysisContext context )
    {
        var method = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IMethodReferenceOperation methodReference => methodReference.Method,
            _ => throw new InvalidOperationException( $"Unexpected operation type '{context.Operation.GetType()}'." )
        };

        if ( method.Name == nameof(SyntaxExtensions.NormalizeWhitespace) )
        {
            var syntax = context.Operation.Syntax;
            var location = syntax.GetLocation();

            if ( syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } )
            {
                location = Location.Create( syntax.SyntaxTree, TextSpan.FromBounds( memberAccess.OperatorToken.SpanStart, syntax.Span.End ) );
            }

            context.ReportDiagnostic( Diagnostic.Create( NormalizeWhitespace, location ) );
        }
    }

    private static void AnalyzeSyntaxNodeWithChains(
        OperationAnalysisContext context,
        INamedTypeSymbol syntaxNodeSymbol,
        INamedTypeSymbol syntaxNodePartialUpdateExtensionsSymbol )
    {
        bool IsSyntaxNodeWithInvocation( IOperation operation, out IInvocationOperation? invocation )
        {
            if ( operation is IInvocationOperation { TargetMethod: var method } invocationOperation &&
                 method.Name.StartsWith( "With", StringComparison.Ordinal ) &&
                 method.ContainingType != null &&
                 context.Compilation.ClassifyConversion( method.ContainingType, syntaxNodeSymbol ) is
                     { IsIdentity: true } or { IsImplicit: true, IsReference: true } &&
                 syntaxNodePartialUpdateExtensionsSymbol.GetMembers( "PartialUpdate" )
                     .OfType<IMethodSymbol>()
                     .SingleOrDefault( m => SymbolEqualityComparer.Default.Equals( m.Parameters[0].Type, method.ContainingType ) ) is { } partialUpdateMethod &&
                 partialUpdateMethod.Parameters.Any(
                     p => method.Name.AsSpan( "With".Length ).Equals( p.Name.AsSpan(), StringComparison.InvariantCultureIgnoreCase ) ) )
            {
                invocation = invocationOperation;

                return true;
            }

            invocation = null;

            return false;
        }

        if ( IsSyntaxNodeWithInvocation( context.Operation, out var lastInvocation ) &&
             !IsSyntaxNodeWithInvocation( context.Operation.Parent, out _ ) )
        {
            var firstInvocation = lastInvocation;

            while ( IsSyntaxNodeWithInvocation( firstInvocation.Instance, out var currentInvocation ) )
            {
                firstInvocation = currentInvocation;
            }

            if ( firstInvocation != lastInvocation )
            {
                var location = context.Operation.Syntax.GetLocation();

                if ( firstInvocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } )
                {
                    location = Location.Create(
                        memberAccess.SyntaxTree,
                        TextSpan.FromBounds( memberAccess.OperatorToken.SpanStart, location.SourceSpan.End ) );
                }

                context.ReportDiagnostic( Diagnostic.Create( SyntaxNodeWith, location ) );
            }
        }
    }

    private static void AnalyzeWithTrivia( OperationAnalysisContext context )
    {
        var method = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IMethodReferenceOperation methodReference => methodReference.Method,
            _ => throw new InvalidOperationException( $"Unexpected operation type '{context.Operation.GetType()}'." )
        };

        if ( method.Name is nameof(SyntaxNodeExtensions.WithLeadingTrivia) or nameof(SyntaxNodeExtensions.WithTrailingTrivia)
            or nameof(SyntaxNodeExtensions.WithTriviaFrom) )
        {
            var syntax = context.Operation.Syntax;
            var location = syntax.GetLocation();

            if ( syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } )
            {
                location = Location.Create( syntax.SyntaxTree, TextSpan.FromBounds( memberAccess.OperatorToken.SpanStart, syntax.Span.End ) );
            }

            context.ReportDiagnostic( Diagnostic.Create( WithTrivia, location ) );
        }
    }
}