// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Patterns.Immutability;
using Metalama.Patterns.Observability.Configuration;
using Microsoft.CodeAnalysis;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

[CompileTime]
internal abstract class GraphBuildingContext
{
    public Assets Assets { get; }

    private readonly ICompilation _compilation;

    protected GraphBuildingContext( ICompilation compilation, Assets assets )
    {
        this.Assets = assets;
        this._compilation = compilation;
    }

    public abstract void ReportDiagnostic( IDiagnostic diagnostic, Location? location = default );

    public abstract bool TreatAsImplementingInpc( ITypeSymbol type );

    public virtual bool CanIgnoreUnobservableExpressions( ISymbol symbol ) => this.GetDependencyAnalysisOptions( symbol ).SuppressWarnings == true;

    protected virtual DependencyAnalysisOptions GetDependencyAnalysisOptions( ISymbol symbol )
    {
        var decl = this._compilation.GetDeclaration( symbol );

        var options = decl switch
        {
            ICompilation compilation => compilation.Enhancements().GetOptions<DependencyAnalysisOptions>(),
            INamespace ns => ns.Enhancements().GetOptions<DependencyAnalysisOptions>(),
            INamedType type => type.Enhancements().GetOptions<DependencyAnalysisOptions>(),
            IMember member => member.Enhancements().GetOptions<DependencyAnalysisOptions>(),
            _ => DependencyAnalysisOptions.Default
        };

        return options;
    }

    public bool IsAutoPropertyOrField( ISymbol symbol ) => this._compilation.GetDeclaration( symbol ) is IFieldOrProperty { IsAutoPropertyOrField: true };

    public bool IsConstant( IMethodSymbol method ) => this.IsConstantMember( method );

    private bool IsConstantMember( ISymbol symbol )
    {
        // Options take precedence over hard-written rules.
        // We have a simple catch-call observability contract at the moment, which conveniently makes all required guarantees.
        var hasContract = this.GetDependencyAnalysisOptions( symbol ).ObservabilityContract != null;

        if ( hasContract )
        {
            return true;
        }

        if ( symbol.Kind is SymbolKind.Property or SymbolKind.Field or SymbolKind.Method )
        {
            return this.IsDeeplyImmutable( symbol.ContainingType );
        }
        else
        {
            return false;
        }
    }

    public bool IsDeeplyImmutable( ITypeSymbol fieldOrPropertyType )
    {
        if ( fieldOrPropertyType is INamedTypeSymbol )
        {
            return ((INamedType) this._compilation.GetDeclaration( fieldOrPropertyType )).GetImmutabilityKind() != ImmutabilityKind.None;
        }
        else
        {
            return fieldOrPropertyType.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.TypeParameter;
        }
    }
}