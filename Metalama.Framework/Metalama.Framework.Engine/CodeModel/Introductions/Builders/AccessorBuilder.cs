// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Accessibility = Metalama.Framework.Code.Accessibility;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed partial class AccessorBuilder : DeclarationBuilder, IMethodBuilderImpl
{
    public IntroducedRef<IMethod> Ref { get; }

    private readonly MemberBuilder _containingMember;

    private Accessibility? _accessibility;
    
    private bool _isIteratorMethod;

    public bool? IsIteratorMethod => this._isIteratorMethod;

    IParameterBuilderList IHasParametersBuilder.Parameters => this.Parameters;

    internal void SetIsIteratorMethod( bool value ) => this._isIteratorMethod = value;

    public AccessorBuilder( MemberBuilder containingDeclaration, MethodKind methodKind, bool isImplicit ) : base( containingDeclaration.AspectLayerInstance )
    {
        this._containingMember = containingDeclaration;
        this._accessibility = null;
        this.MethodKind = methodKind;
        this.IsImplicitlyDeclared = isImplicit;
        this.Ref = new IntroducedRef<IMethod>( this.Compilation.RefFactory );
    }

    TypeParameterBuilderList IMethodBuilderImpl.TypeParameters => TypeParameterBuilderList.Empty;

    IParameterBuilder IMethodBuilder.ReturnParameter => this.ReturnParameter;

    [Memo]
    public BaseParameterBuilder ReturnParameter
        => (containingDeclaration: this.ContainingDeclaration, this.MethodKind) switch
        {
            (PropertyBuilder or IndexerBuilder, MethodKind.PropertyGet) => new PropertyGetReturnParameterBuilder( this ),
            (PropertyBuilder or IndexerBuilder, MethodKind.PropertySet) => new VoidReturnParameterBuilder( this ),
            (FieldBuilder, MethodKind.PropertyGet) => new PropertyGetReturnParameterBuilder( this ),
            (FieldBuilder, MethodKind.PropertySet) => new VoidReturnParameterBuilder( this ),
            (EventBuilder, _) => new EventReturnParameterBuilder( this ),
            _ => throw new AssertionFailedException( $"Unexpected combination ('{this.ContainingDeclaration}', {this.MethodKind})." )
        };

    public IType ReturnType
    {
        get => this.ReturnParameter.Type;
        set => throw new NotSupportedException( "Cannot directly change the return type of an accessor." );
    }

    [Memo]
    public ITypeParameterList TypeParameters => TypeParameterList.Empty;

    public IReadOnlyList<IType> TypeArguments => ImmutableArray<IType>.Empty;

    public override bool IsImplicitlyDeclared { get; }

    public bool IsGeneric => false;

    public bool IsCanonicalGenericInstance => true;

    public IMethod? OverriddenMethod
        => (containingDeclaration: this.ContainingDeclaration, this.MethodKind) switch
        {
            (PropertyBuilder propertyBuilder, MethodKind.PropertyGet) => propertyBuilder.OverriddenProperty?.GetMethod.AssertNotNull(),
            (PropertyBuilder propertyBuilder, MethodKind.PropertySet) => propertyBuilder.OverriddenProperty?.SetMethod.AssertNotNull(),
            (IndexerBuilder indexerBuilder, MethodKind.PropertyGet) => indexerBuilder.OverriddenIndexer?.GetMethod.AssertNotNull(),
            (IndexerBuilder indexerBuilder, MethodKind.PropertySet) => indexerBuilder.OverriddenIndexer?.SetMethod.AssertNotNull(),
            (FieldBuilder _, _) => null,
            (EventBuilder eventBuilder, MethodKind.EventAdd) => eventBuilder.OverriddenEvent?.AddMethod.AssertNotNull(),
            (EventBuilder eventBuilder, MethodKind.EventRemove) => eventBuilder.OverriddenEvent?.RemoveMethod.AssertNotNull(),
            _ => throw new AssertionFailedException( $"Unexpected combination ('{this.ContainingDeclaration}', {this.MethodKind})." )
        };

    // The cast is intentional, IParameterList must be implemented by all values.
    IParameterList IHasParameters.Parameters => (IParameterList) this.Parameters;

    [Memo]
    public IParameterBuilderList Parameters
        => (ContainingMember: this._containingMember, this.MethodKind) switch
        {
            // TODO: Indexer parameters (need to have special IParameterList implementation that would mirror adding parameters to the indexer property).
            (IIndexer, MethodKind.PropertyGet) => new IndexerAccessorParameterBuilderList( this ),
            (IIndexer, MethodKind.PropertySet) => new IndexerAccessorParameterBuilderList( this ),
            (IProperty, MethodKind.PropertyGet) => new ParameterBuilderList(),
            (IProperty, MethodKind.PropertySet) =>
                new ParameterBuilderList( [new PropertySetValueParameterBuilder( this, 0 )] ),
            (FieldBuilder _, MethodKind.PropertyGet) => new ParameterBuilderList(),
            (FieldBuilder _, MethodKind.PropertySet) => new ParameterBuilderList( [new PropertySetValueParameterBuilder( this, 0 )] ),
            (IEvent _, _) =>
                new ParameterBuilderList( [new EventValueParameterBuilder( this )] ),
            _ => throw new AssertionFailedException( $"Unexpected combination ('{this.ContainingDeclaration}', {this.MethodKind})." )
        };

    public MethodKind MethodKind { get; }

    bool IMethodBuilder.IsReadOnly { get; set; }

    public OperatorKind OperatorKind => OperatorKind.None;

    IMethod IMethod.Definition => this;

    IMember IMember.Definition => this;

    IMemberOrNamedType IMemberOrNamedType.Definition => this;

    public bool IsPartial
    {
        get => this._containingMember.IsPartial;
        set => throw new InvalidOperationException( "Accessor's IsPartial cannot be directly set." );
    }

    public bool HasImplementation => true;

    public bool IsExtern
    {
        get => this._containingMember.IsExtern;
        set => throw new InvalidOperationException( "Accessor's IsExtern cannot be directly set." );
    }

    public IMethodInvoker With( InvokerOptions options ) => new MethodInvoker( this, options );

    public IMethodInvoker With( object? target, InvokerOptions options ) => new MethodInvoker( this, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new MethodInvoker( this ).CreateInvokeExpression( args );

    public object? Invoke( params object?[] args ) => new MethodInvoker( this ).Invoke( args );

    public object? Invoke( IEnumerable<IExpression> args ) => new MethodInvoker( this ).Invoke( args );

    public Accessibility Accessibility
    {
        get => this._accessibility ?? this._containingMember.Accessibility;

        set
        {
            if ( this.ContainingDeclaration is FieldBuilder )
            {
                throw new InvalidOperationException( "Cannot change field pseudo accessor accessibility." );
            }

            if ( this.ContainingDeclaration is not PropertyBuilder propertyBuilder )
            {
                throw new InvalidOperationException( $"Cannot change event accessor accessibility." );
            }

            if ( value == Accessibility.Undefined )
            {
                this._accessibility = null;

                return;
            }

            if ( !value.IsSubsetOrEqual( propertyBuilder.Accessibility ) )
            {
                throw new InvalidOperationException(
                    $"Cannot change accessor accessibility to {value}, which is not more restrictive than parent accessibility {propertyBuilder.Accessibility}." );
            }

            var otherAccessor = this.MethodKind switch
            {
                MethodKind.PropertyGet => propertyBuilder.SetMethod,
                MethodKind.PropertySet => propertyBuilder.GetMethod,
                _ => throw new AssertionFailedException( $"Unexpected MethodKind: {this.MethodKind}." )
            };

            if ( value != propertyBuilder.Accessibility && otherAccessor == null )
            {
                throw new InvalidOperationException( $"Cannot change accessor accessibility, if the property has a single accessor ." );
            }

            if ( value != propertyBuilder.Accessibility && otherAccessor != null
                                                        && otherAccessor.Accessibility.IsSubsetOf( propertyBuilder.Accessibility ) )
            {
                throw new InvalidOperationException(
                    $"Cannot change accessor accessibility to {value}, because the other accessor is already restricted to {otherAccessor.Accessibility}." );
            }

            this._accessibility = value;
        }
    }

    public string Name
    {
        get
        {
            // The name of indexer is "this[]", but the names of its accessors should be e.g. "get_Item".
            var parentName = this._containingMember is IndexerBuilder ? "Item" : this._containingMember.Name;

            return this.MethodKind switch
            {
                MethodKind.PropertyGet => $"get_{parentName}",
                MethodKind.PropertySet => $"set_{parentName}",
                MethodKind.EventAdd => $"add_{parentName}",
                MethodKind.EventRemove => $"remove_{parentName}",
                _ => throw new AssertionFailedException( $"Unexpected MethodKind: {this.MethodKind}." )
            };
        }

        set => throw new NotSupportedException();
    }

    public bool IsStatic
    {
        get => this._containingMember.IsStatic;
        set => throw new NotSupportedException( "Cannot directly change staticity of an accessor." );
    }

    public bool IsVirtual
    {
        get => this._containingMember.IsVirtual;
        set => throw new NotSupportedException( "Cannot directly change the IsVirtual property of an accessor." );
    }

    public bool IsSealed
    {
        get => this._containingMember.IsSealed;
        set => throw new NotSupportedException( "Cannot directly change the IsSealed property of an accessor." );
    }

    public bool IsAbstract
    {
        get => this._containingMember.IsAbstract;
        set => throw new InvalidOperationException();
    }

    public bool IsReadOnly => false;

    public bool IsOverride => this._containingMember.IsOverride;

    public bool IsNew => this._containingMember.IsNew;

    public bool? HasNewKeyword => false;

    public bool IsAsync => false;

    public INamedType DeclaringType => this._containingMember.DeclaringType;

    public override bool IsDesignTimeObservable => this._containingMember.IsDesignTimeObservable;

    public override IDeclaration ContainingDeclaration => this._containingMember;

    public override DeclarationKind DeclarationKind => DeclarationKind.Method;

    IParameter IMethod.ReturnParameter => this.ReturnParameter;

    IType IMethod.ReturnType => this.ReturnType;

    Accessibility IMemberOrNamedType.Accessibility => this.Accessibility;

    string INamedDeclaration.Name => this.Name;

    bool IMemberOrNamedType.IsStatic => this.IsStatic;

    bool IMember.IsVirtual => this.IsVirtual;

    bool IMemberOrNamedType.IsSealed => this.IsSealed;

    public ITypeParameterBuilder AddTypeParameter( string name ) => throw new NotSupportedException( "Cannot add generic parameters to accessors." );

    public IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
        => throw new NotSupportedException( "Cannot directly add parameters to accessors." );

    public IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
        => throw new NotSupportedException( "Cannot directly add parameters to accessors." );

    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations
        => (containingDeclaration: this.ContainingDeclaration, this.MethodKind) switch
        {
            (PropertyBuilder propertyBuilder, MethodKind.PropertyGet)
                => propertyBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.GetMethod ).AssertNoneNull(),
            (PropertyBuilder propertyBuilder, MethodKind.PropertySet)
                => propertyBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.SetMethod ).AssertNoneNull(),
            (IndexerBuilder indexerBuilder, MethodKind.PropertyGet)
                => indexerBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.GetMethod ).AssertNoneNull(),
            (IndexerBuilder indexerBuilder, MethodKind.PropertySet)
                => indexerBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.SetMethod ).AssertNoneNull(),
            (FieldBuilder _, _) => Array.Empty<IMethod>(),
            (EventBuilder eventBuilder, MethodKind.EventAdd)
                => eventBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.AddMethod ),
            (EventBuilder eventBuilder, MethodKind.EventRemove)
                => eventBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( p => p.RemoveMethod ),
            _ => throw new AssertionFailedException( $"Unexpected combination ('{this.ContainingDeclaration}', {this.MethodKind})." )
        };

    public bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public MethodInfo ToMethodInfo() => throw new NotImplementedException();

    IHasAccessors IMethod.DeclaringMember => (IHasAccessors) this._containingMember;

    public MethodBase ToMethodBase() => throw new NotImplementedException();

    public MemberInfo ToMemberInfo() => throw new NotImplementedException();

    ExecutionScope IMemberOrNamedType.ExecutionScope => ExecutionScope.RunTime;

    public IMember? OverriddenMember => (IMemberImpl?) this.OverriddenMethod;

    public override bool CanBeInherited => this.IsVirtual && !this.IsSealed && ((IDeclarationImpl) this.DeclaringType).CanBeInherited;

    protected override void FreezeChildren()
    {
        base.FreezeChildren();
        this.ReturnParameter.Freeze();

        foreach ( var parameter in this.Parameters )
        {
            parameter.Freeze();
        }
    }

    IRef<IMethodBase> IMethodBase.ToRef() => this.Ref;

    IMethod IMethod.MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotSupportedException();

    IRef<IMethod> IMethod.ToRef() => this.Ref;

    IRef<IMember> IMember.ToRef() => this.Ref;

    IRef<IMemberOrNamedType> IMemberOrNamedType.ToRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    protected override void EnsureReferenceInitialized() => this.Ref.BuilderData = new MethodBuilderData( this, this.ContainingDeclaration.ToFullRef() );
}