// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal abstract class MethodBaseBuilder : MemberBuilder, IMethodBaseBuilder, IMethodBaseImpl
{
    public ParameterBuilderList Parameters { get; } = [];

    /// <summary>
    /// Gets a value indicating whether <see cref="InsertParameter(int, string, IType, RefKind, TypedConstant?)"/> was called, which shifts template parameter indices.
    /// </summary>
    internal bool HasInsertedParameters { get; private set; }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        foreach ( var parameter in this.Parameters )
        {
            parameter.Freeze();
        }
    }

    public IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        var parameter = new ParameterBuilder( this, this.Parameters.Count, name, type, refKind, this.AspectLayerInstance );
        parameter.DefaultValue = defaultValue;
        this.Parameters.Add( parameter );

        return parameter;
    }

    public IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        var iType = this.Compilation.Factory.GetTypeByReflectionType( type );
        TypedConstant? typedConstant = defaultValue != null ? TypedConstant.Create( defaultValue.Value.Value, iType ) : null;

        return this.AddParameter( name, iType, refKind, typedConstant );
    }

    public IParameterBuilder InsertParameter( int index, string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        if ( index < 0 || index > this.Parameters.Count )
        {
            throw new ArgumentOutOfRangeException( nameof(index) );
        }

        var parameter = new ParameterBuilder( this, index, name, type, refKind, this.AspectLayerInstance );
        parameter.DefaultValue = defaultValue;
        this.Parameters.Insert( index, parameter );

        // Update indices of parameters after the insertion point.
        for ( var i = index + 1; i < this.Parameters.Count; i++ )
        {
            ((ParameterBuilder) this.Parameters[i]).SetIndex( i );
        }

        this.HasInsertedParameters = true;

        return parameter;
    }

    public IParameterBuilder InsertParameter( int index, string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        var iType = this.Compilation.Factory.GetTypeByReflectionType( type );
        TypedConstant? typedConstant = defaultValue != null ? TypedConstant.Create( defaultValue.Value.Value, iType ) : null;

        return this.InsertParameter( index, name, iType, refKind, typedConstant );
    }

    IParameterList IHasParameters.Parameters => this.Parameters;

    IParameterBuilderList IHasParametersBuilder.Parameters => this.Parameters;

    public abstract MethodBase ToMethodBase();

    IRef<IMethodBase> IMethodBase.ToRef() => throw new NotSupportedException();

    protected MethodBaseBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType declaringType,
        string name )
        : base( declaringType, name, aspectLayerInstance ) { }
}