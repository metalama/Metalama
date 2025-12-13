// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Analyzers;

/// <summary>
/// Analyzer to detect potentially unsafe calls to SyntaxFactory.Identifier and SyntaxFactory.IdentifierName
/// with dynamic names that may be C# keywords and should use SyntaxFactoryEx.SafeIdentifier/SafeIdentifierName instead.
/// </summary>
[DiagnosticAnalyzer( LanguageNames.CSharp )]
[UsedImplicitly]
public class UnsafeIdentifierAnalyzer : DiagnosticAnalyzer
{
    // Range: 0850-0859
    internal static readonly DiagnosticDescriptor UnsafeIdentifierCall = new(
        "LAMA0850",
        "Potentially unsafe Identifier/IdentifierName call",
        "Call to SyntaxFactory.{0} with dynamic name may produce invalid code if the name is a C# keyword. Consider using SyntaxFactoryEx.Safe{0} or SyntaxFactoryEx.WellKnown{0} instead.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( UnsafeIdentifierCall );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction( InitializeCompilation );
    }

    private static void InitializeCompilation( CompilationStartAnalysisContext context )
    {
        var syntaxFactorySymbol = context.Compilation.GetTypeByMetadataName( "Microsoft.CodeAnalysis.CSharp.SyntaxFactory" );

        if ( syntaxFactorySymbol == null )
        {
            return;
        }

        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation( operationContext, syntaxFactorySymbol ),
            OperationKind.Invocation );
    }

    private static void AnalyzeInvocation( OperationAnalysisContext context, INamedTypeSymbol syntaxFactorySymbol )
    {
        if ( context.Operation is not IInvocationOperation invocation )
        {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if it's SyntaxFactory.Identifier or SyntaxFactory.IdentifierName
        if ( !SymbolEqualityComparer.Default.Equals( method.ContainingType, syntaxFactorySymbol ) )
        {
            return;
        }

        if ( method.Name is not ("Identifier" or "IdentifierName") )
        {
            return;
        }

        // Check if the first argument is a dynamic name (property access like .Name or variable)
        if ( invocation.Arguments.Length == 0 )
        {
            return;
        }

        var firstArg = invocation.Arguments[0].Value;

        // Skip if it's a string literal (constant value) - those are intentional hardcoded names
        if ( firstArg.ConstantValue.HasValue )
        {
            return;
        }

        // Skip if it's a nameof expression (also safe)
        if ( firstArg is INameOfOperation )
        {
            return;
        }

        // Report warning for dynamic names that could be keywords
        context.ReportDiagnostic(
            Diagnostic.Create(
                UnsafeIdentifierCall,
                invocation.Syntax.GetLocation(),
                method.Name ) );
    }
}
