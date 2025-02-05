// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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