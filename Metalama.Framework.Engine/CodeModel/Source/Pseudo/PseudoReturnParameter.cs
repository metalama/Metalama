// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using RefKind = Metalama.Framework.Code.RefKind;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo;

internal class PseudoReturnParameter : BaseDeclaration, IParameterImpl
{
    private readonly SourceMethod _declaringMethod;
    private readonly IMethodSymbol _methodSymbol;

    public PseudoReturnParameter( SourceMethod declaringMethod, IMethodSymbol methodSymbol )
    {
        this._methodSymbol = methodSymbol;
        this._declaringMethod = declaringMethod;
    }

    public RefKind RefKind => this._methodSymbol.RefKind.ToOurRefKind();

    public string Name => "<return>";

    public int Index => -1;

    TypedConstant? IParameter.DefaultValue => default;

    public bool IsParams => false;

    public bool IsThis => false;

    public IHasParameters DeclaringMember => this._declaringMethod;

    public ParameterInfo ToParameterInfo() => CompileTimeReturnParameterInfo.Create( this );

    public bool IsReturnParameter => true;

    public override IAssembly DeclaringAssembly => this._declaringMethod.DeclaringAssembly;

    IDeclarationOrigin IDeclaration.Origin => this._declaringMethod.Origin;

    public override IDeclaration ContainingDeclaration => this._declaringMethod;

    public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

    public override CompilationModel Compilation => this.ContainingDeclaration.AssertNotNull().GetCompilationModel();

    public override Location? DiagnosticLocation => this._declaringMethod.GetDiagnosticLocation();

    public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => this._declaringMethod.DeclaringSyntaxReferences;

    public override bool CanBeInherited => this._declaringMethod.CanBeInherited;

    public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
        => this._declaringMethod.ToDisplayString( format, context ) + "/" + this.Name;

    public override IDeclarationOrigin Origin => this._declaringMethod.Origin;

    protected override int GetHashCodeCore() => this._declaringMethod.GetHashCode() + 7;

    bool IExpression.IsAssignable => throw new NotSupportedException( "Cannot use the return parameter as an expression." );

    public ref object? Value => throw new NotSupportedException( "Cannot use the return parameter as an expression." );

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        => throw new NotSupportedException( "Cannot use the return parameter as an expression." );

    public override bool BelongsToCurrentProject => this.ContainingDeclaration.BelongsToCurrentProject;

    [Memo]
    private IFullRef<IParameter> Ref
        => this.RefFactory.FromSymbol<IParameter>(
            this._declaringMethod.GetSymbol().AssertSymbolNotNull(),
            this._declaringMethod.GenericContextForSymbolMapping,
            RefTargetKind.Return );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<IParameter> IParameter.ToRef() => this.Ref;

    internal override GenericContext GenericContext => (GenericContext) this.ContainingDeclaration.GenericContext;

    internal override DeclarationImplementationKind ImplementationKind => DeclarationImplementationKind.Pseudo;

    public IType Type => this._declaringMethod.ReturnType;

    public override bool Equals( IDeclaration? other )
        => other is PseudoReturnParameter methodReturnParameter &&
           this._methodSymbol.Equals( methodReturnParameter._methodSymbol );

    public override bool IsImplicitlyDeclared => this._declaringMethod.IsImplicitlyDeclared;

    public override ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;

    internal override ICompilationElement? Translate(
        CompilationModel newCompilation,
        IGenericContext? genericContext = null,
        Type? interfaceType = null )
        => ((IMethod?) this._declaringMethod.Translate( newCompilation, genericContext ))?.ReturnParameter;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        => this._declaringMethod.GetDerivedDeclarations( options ).Select( d => ((IMethod) d).ReturnParameter );

    [Memo]
    public override IAttributeCollection Attributes
        => new AttributeCollection(
            this,
            this._methodSymbol.GetReturnTypeAttributes()
                .Select( a => new SymbolAttributeRef( a, this.ToFullDeclarationRef(), this.Compilation.RefFactory ) )
                .ToReadOnlyList() );

    public override SyntaxTree? PrimarySyntaxTree => this._declaringMethod.PrimarySyntaxTree;
}