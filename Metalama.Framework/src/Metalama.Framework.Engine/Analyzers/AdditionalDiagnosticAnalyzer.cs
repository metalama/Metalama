// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Analyzers;

#pragma warning disable RS1001, RS1022

/// <summary>
/// A <see cref="DiagnosticAnalyzer"/> that does not require the Metalama pipeline and validates other rules identically in all processes.
/// </summary>
public sealed class AdditionalDiagnosticAnalyzer : DiagnosticAnalyzer
{
    static AdditionalDiagnosticAnalyzer()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    public override void Initialize( AnalysisContext context )
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
        context.RegisterSymbolAction( this.AnalyzeNamedTypeSymbol, SymbolKind.NamedType );
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        AdditionalDiagnosticDescriptors.CannotImplementBecauseOfInternalImplementAttribute.ToRoslynDescriptor() );

    private void AnalyzeNamedTypeSymbol( SymbolAnalysisContext context ) => this.AnalyzeNamedTypeSymbol( new SymbolAnalysisContextAdapter( context ) );

    internal void AnalyzeNamedTypeSymbol( ISymbolAnalysisContext context )
    {
        var namedType = (INamedTypeSymbol) context.Symbol;

        if ( namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface) )
        {
            return;
        }

        var currentAssembly = namedType.ContainingAssembly;

        // Since [InternalImplement] is (logically) inherited and does not need to be repeated on child interfaces,
        // we must test all interfaces and not just first-level ones.

        if ( !namedType.AllInterfaces.IsDefaultOrEmpty )
        {
            foreach ( var @interface in namedType.AllInterfaces )
            {
                if ( @interface.ContainingAssembly.Equals( currentAssembly ) )
                {
                    // Cheap test: do not consider interfaces in the current assembly.
                    continue;
                }

                var attributes = @interface.GetAttributes();

                if ( !attributes.IsDefaultOrEmpty )
                {
                    foreach ( var attribute in attributes )
                    {
                        // Check the attribute name.
                        if ( attribute.AttributeClass is { Name: nameof(InternalImplementAttribute) }
                             && attribute.AttributeClass.GetReflectionFullName() == typeof(InternalImplementAttribute).FullName )
                        {
                            if ( @interface.ContainingAssembly.AreInternalsVisibleToImpl( currentAssembly ) )
                            {
                                continue;
                            }

                            context.ReportDiagnostic(
                                AdditionalDiagnosticDescriptors
                                    .CannotImplementBecauseOfInternalImplementAttribute
                                    .CreateRoslynDiagnostic(
                                        context.Symbol.GetDiagnosticLocation(),
                                        (attribute.AttributeClass, attribute.AttributeClass.ContainingAssembly.Name) ) );

                            return;
                        }
                    }
                }
            }
        }
    }
}