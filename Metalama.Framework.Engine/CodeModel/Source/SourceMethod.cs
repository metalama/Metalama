// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoslynMethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class SourceMethod : SourceMethodBase, IMethodImpl
{
    public SourceMethod( IMethodSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
        symbol,
        compilation,
        genericContextForSymbolMapping )
    {
        Invariant.Assert(
            symbol.MethodKind is not (RoslynMethodKind.Constructor or RoslynMethodKind.StaticConstructor),
            "Cannot use the Method class for constructors or accessors." );

        Invariant.Assert(
            symbol.PartialDefinitionPart == null,
            "Cannot use partial implementation to instantiate the SourceMethod class." );
    }

    [Memo]
    public IParameter ReturnParameter => new PseudoReturnParameter( this, this.MethodSymbol );

    [Memo]
    public IType ReturnType => this.Compilation.Factory.GetIType( this.MethodSymbol.ReturnType, this.GenericContextForSymbolMapping );

    [Memo]
    public ITypeParameterList TypeParameters
        => new TypeParameterList(
            this,
            this.MethodSymbol.TypeParameters.Select( x => this.RefFactory.FromSymbol<ITypeParameter>( x, this.GenericContextForSymbolMapping ) )
                .ToReadOnlyList() );

    [Memo]
    public IReadOnlyList<IType> TypeArguments
        => this.MethodSymbol.TypeArguments.Select( t => this.Compilation.Factory.GetIType( t, this.GenericContextForSymbolMapping ) ).ToImmutableArray();

    public override DeclarationKind DeclarationKind => DeclarationKind.Method;

    public OperatorKind OperatorKind => this.MethodSymbol.GetOperatorKind();

    protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

    [Memo]
    public IMethod Definition
        => this.MethodSymbol == this.MethodSymbol.OriginalDefinition ? this : this.Compilation.Factory.GetMethod( this.MethodSymbol.OriginalDefinition );

    IMemberOrNamedType IMemberOrNamedType.Definition => this.Definition;

    public override bool IsPartial => this.MethodSymbol.IsPartialDefinition || this.MethodSymbol.PartialDefinitionPart != null;

    public bool IsExtern => this.MethodSymbol.IsExtern;

    public IMethodInvoker With( InvokerOptions options ) => new MethodInvoker( this, options );

    public IMethodInvoker With( object? target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new MethodInvoker( this ).CreateInvokeExpression( args );

    public object? Invoke( params object?[] args ) => new MethodInvoker( this ).Invoke( args );

    public object? Invoke( IEnumerable<IExpression> args ) => new MethodInvoker( this ).Invoke( args );

    public bool IsGeneric => this.MethodSymbol.TypeParameters.Length > 0;

    public IMethod MakeGenericInstance( IReadOnlyList<IType> typeArguments )
    {
        if ( typeArguments.Any( GenericContext.ReferencesAnyIntroducedType ) )
        {
            var genericContext = new IntroducedGenericContext(
                typeArguments.SelectAsImmutableArray( t => t.ToFullRef() ),
                this.Ref.DefinitionRef,
                (IntroducedGenericContext?) ((SourceNamedType) this.DeclaringType).GenericContextForSymbolMapping );

            return this.Compilation.Factory.GetMethod( this.MethodSymbol, genericContext );
        }
        else
        {
            var mappedMethod = this.MethodSymbol.Construct(
                typeArguments.SelectAsArray( a => a.GetSymbol().AssertSymbolNotNull() ) );

            return this.Compilation.Factory.GetMethod( mappedMethod );
        }
    }

    public bool IsReadOnly => this.MethodSymbol.IsReadOnly;

    public override bool IsExplicitInterfaceImplementation => !this.MethodSymbol.ExplicitInterfaceImplementations.IsEmpty;

    protected override IRef<IMember> ToMemberRef() => this.Ref;

    [Memo]
    public override bool IsAsync
        => this.MethodSymbol.MetadataToken == 0
            ? this.MethodSymbol.IsAsync
            : this.MethodSymbol.GetAttributes()
                .Any( a => a.AttributeConstructor?.ContainingType.Name is nameof(AsyncStateMachineAttribute) or nameof(AsyncIteratorStateMachineAttribute) );

    public IMethod? OverriddenMethod
    {
        get
        {
            var overriddenMethod = this.MethodSymbol.OverriddenMethod;

            if ( overriddenMethod != null )
            {
                return this.Compilation.Factory.GetMethod( overriddenMethod, this.GenericContextForSymbolMapping );
            }
            else
            {
                return null;
            }
        }
    }

    [Memo]
    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations
        => ((IMethodSymbol) this.Symbol).ExplicitInterfaceImplementations
            .Select( m => this.Compilation.Factory.GetMethod( m, this.GenericContextForSymbolMapping ) )
            .ToReadOnlyList();

    public MethodInfo ToMethodInfo() => CompileTimeMethodInfo.Create( this );

    public IHasAccessors? DeclaringMember
        => this.MethodSymbol.AssociatedSymbol != null
            ? this.Compilation.Factory.GetDeclaration( this.MethodSymbol.AssociatedSymbol ) as IHasAccessors
            : null;

    public override MethodBase ToMethodBase() => this.ToMethodInfo();

    public override IMember? OverriddenMember => this.OverriddenMethod;

    public bool IsCanonicalGenericInstance => ReferenceEquals( this.Symbol.OriginalDefinition, this.Symbol );

    public bool? IsIteratorMethod => this.MethodSymbol.IsIteratorMethod();

    [Memo]
    public override ImmutableArray<SourceReference> Sources => this.GetSourcesImpl();

    private ImmutableArray<SourceReference> GetSourcesImpl()
    {
        if ( this.MethodSymbol.PartialImplementationPart != null )
        {
            var sources = ImmutableArray.CreateBuilder<SourceReference>( 2 );
            sources.Add( new SourceReference( this.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax(), SourceReferenceImpl.Instance ) );

            sources.Add(
                new SourceReference( this.MethodSymbol.PartialImplementationPart.DeclaringSyntaxReferences[0].GetSyntax(), SourceReferenceImpl.Instance ) );

            return sources.MoveToImmutable();
        }
        else
        {
            return base.Sources;
        }
    }

    [Memo]
    private IFullRef<IMethod> Ref => this.RefFactory.FromSymbolBasedDeclaration<IMethod>( this );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public new IRef<IMethod> ToRef() => this.Ref;

    protected override IFullRef<IMethodBase> GetMethodBaseRef() => this.Ref;

    protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
}