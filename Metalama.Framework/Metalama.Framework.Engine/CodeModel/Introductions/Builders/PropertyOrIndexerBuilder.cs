// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Reflection;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal abstract class PropertyOrIndexerBuilder : MemberBuilder, IPropertyOrIndexerBuilder, IPropertyOrIndexerImpl
{
    private IType _type;

    public bool HasInitOnlySetter { get; protected set; }

    public RefKind RefKind { get; set; }

    public AccessorBuilder? GetMethod { get; }

    public AccessorBuilder? SetMethod { get; }

    IRef<IFieldOrPropertyOrIndexer> IFieldOrPropertyOrIndexer.ToRef() => throw new NotSupportedException();

    public abstract Writeability Writeability { get; set; }

    public IType Type
    {
        get => this._type;
        set
        {
            this.CheckNotFrozen();

            Invariant.Assert( value is not { TypeKind: TypeKind.Dynamic } );

            this._type = this.Translate( value );
        }
    }

    IMethod? IFieldOrPropertyOrIndexer.GetMethod => this.GetMethod;

    IMethodBuilder? IPropertyOrIndexerBuilder.GetMethod => this.GetMethod;

    IMethod? IFieldOrPropertyOrIndexer.SetMethod => this.SetMethod;

    IMethodBuilder? IPropertyOrIndexerBuilder.SetMethod => this.SetMethod;

    protected PropertyOrIndexerBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType targetType,
        string name,
        bool hasGetter,
        bool hasSetter,
        bool hasImplicitGetter,
        bool hasImplicitSetter )
        : base( targetType, name, aspectLayerInstance )
    {
        Invariant.Assert( hasGetter || hasSetter );

        this._type = targetType.Compilation.GetCompilationModel().Cache.SystemObjectType;

        if ( hasGetter )
        {
            this.GetMethod = new AccessorBuilder( this, MethodKind.PropertyGet, hasImplicitGetter );
        }

        if ( hasSetter )
        {
            this.SetMethod = new AccessorBuilder( this, MethodKind.PropertySet, hasImplicitSetter );
        }

        this.HasInitOnlySetter = false;
    }

    public IMethod? GetAccessor( MethodKind methodKind ) => this.GetAccessorImpl( methodKind );

    public IEnumerable<IMethod> Accessors
    {
        get
        {
            if ( this.GetMethod != null )
            {
                yield return this.GetMethod;
            }

            if ( this.SetMethod != null )
            {
                yield return this.SetMethod;
            }
        }
    }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        this.GetMethod?.Freeze();
        this.SetMethod?.Freeze();
    }

    public PropertyInfo ToPropertyInfo() => CompileTimePropertyInfo.Create( this );

    IRef<IPropertyOrIndexer> IPropertyOrIndexer.ToRef() => throw new NotSupportedException();
}