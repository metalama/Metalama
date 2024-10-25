// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.IO;

namespace Metalama.Framework.Engine;

#pragma warning disable RS1001 // Missing diagnostic analyzer attribute
#pragma warning disable RS1022 // Do not use types from Workspaces assembly in an analyzer

internal class GeneratedCodeAnalyzer : DiagnosticAnalyzer
{
    private const string _diagnosticCategory = "Metalama.GeneratedCodeAnalyzer";

    internal static readonly DiagnosticDefinition<(string AspectType, ISymbol Target, string Addendum)> AspectAppliedToGeneratedCode = new(
        "LAMA0320",
        "Aspect can't be applied to source generated code.",
        "The aspect '{0}' can't be applied to '{1}', because it's in source generated code.{2}",
        _diagnosticCategory,
        Severity.Warning );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( AspectAppliedToGeneratedCode.ToRoslynDescriptor() );

    public override void Initialize( AnalysisContext context )
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );

        context.RegisterSymbolAction( this.AnalyzeSymbol, SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.NamedType, SymbolKind.Parameter, SymbolKind.Property );
    }

    private void AnalyzeSymbol( SymbolAnalysisContext context )
    {
        var tree = context.Symbol.GetClosestPrimaryDeclarationSyntax()?.SyntaxTree;

        if ( MetalamaCompilerInfo.IsActive )
        {
            if ( tree == null || !SourceGeneratedCodeTracker.IsGenerated( tree ) )
            {
                return;
            }
        }
        else
        {
            // At design time, source generated files have relative paths, other files seem to have absolute paths.
            if ( !context.IsGeneratedCode || Path.IsPathRooted( tree?.FilePath ) )
            {
                return;
            }
        }

        var iAspect = context.Compilation.GetTypeByMetadataName( typeof( IAspect ).FullName! );

        var symbol = context.Symbol;

        foreach ( var attribute in symbol.GetAttributes() )
        {
            if ( context.Compilation.HasImplicitConversion( attribute.AttributeClass, iAspect ) )
            {
                var location = attribute.GetDiagnosticLocation() ?? symbol.GetDiagnosticLocation();

                var addendum = string.Empty;

                if ( location != null && Path.GetExtension( location.GetMappedLineSpan().Path ) is ".razor" or ".cshtml" )
                {
                    addendum = " For Razor files, consider extracting the relevant code to code behind.";
                }

                var diagnostic = AspectAppliedToGeneratedCode.CreateRoslynDiagnostic(
                    location,
                    (AttributeHelper.GetShortName( attribute.AttributeClass!.MetadataName ), symbol, addendum) );

                context.ReportDiagnostic( diagnostic );
            }
        }
    }
}