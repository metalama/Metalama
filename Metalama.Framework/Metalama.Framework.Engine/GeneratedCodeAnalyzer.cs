// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
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

[UsedImplicitly]
internal class GeneratedCodeAnalyzer : DiagnosticAnalyzer
{
    private const string _diagnosticCategory = "Metalama.GeneratedCodeAnalyzer";

    private static readonly DiagnosticDefinition<(string AspectType, ISymbol Target, string Addendum)> _aspectAppliedToGeneratedCode = new(
        "LAMA0320",
        "Aspect can't be applied to source generated code.",
        "The aspect '{0}' can't be applied to '{1}', because it's in source generated code.{2}",
        _diagnosticCategory,
        Severity.Warning );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( _aspectAppliedToGeneratedCode.ToRoslynDescriptor() );

    public override void Initialize( AnalysisContext context )
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );

        context.RegisterSymbolAction( AnalyzeSymbol, SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.NamedType, SymbolKind.Parameter, SymbolKind.Property );
    }

    private static void AnalyzeSymbol( SymbolAnalysisContext context )
    {
        var tree = context.Symbol.GetClosestPrimaryDeclarationSyntax()?.SyntaxTree;

        if ( tree == null )
        {
            return;
        }

        var isGenerated = SourceGeneratedCodeTracker.IsGenerated( tree );

        if ( isGenerated == false )
        {
            // Based on information from Metalama Compiler, this is not a source generated file.
            return;
        }
        else if ( isGenerated == null )
        {
            // At design time, source generated files have relative paths, other files seem to have absolute paths.
            if ( !context.IsGeneratedCode || Path.IsPathRooted( tree.FilePath ) )
            {
                return;
            }
        }

        var iAspect = context.Compilation.GetTypeByMetadataName( typeof(IAspect).FullName! );

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

                var diagnostic = _aspectAppliedToGeneratedCode.CreateRoslynDiagnostic(
                    location,
                    (AttributeHelper.GetShortName( attribute.AttributeClass!.MetadataName ), symbol, addendum) );

                context.ReportDiagnostic( diagnostic );
            }
        }
    }
}