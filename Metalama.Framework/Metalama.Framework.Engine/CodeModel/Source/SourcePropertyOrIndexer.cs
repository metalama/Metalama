// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;
using Accessibility = Metalama.Framework.Code.Accessibility;
using IPropertyOrIndexer = Metalama.Framework.Code.IPropertyOrIndexer;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal abstract class SourcePropertyOrIndexer : SourceMember, IPropertyOrIndexer, IFieldOrPropertyOrIndexerImpl
{
    protected IPropertySymbol PropertySymbol { get; }

    protected SourcePropertyOrIndexer( IPropertySymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
        compilation,
        genericContextForSymbolMapping )
    {
        this.PropertySymbol = symbol.AssertBelongsToCompilationContext( compilation.CompilationContext );
    }

    public override ISymbol Symbol => this.PropertySymbol;

    public RefKind RefKind => this.PropertySymbol.RefKind.ToOurRefKind();

    public override bool IsExplicitInterfaceImplementation => !this.PropertySymbol.ExplicitInterfaceImplementations.IsEmpty;

    [Memo]
    public IType Type => this.Compilation.Factory.GetIType( this.PropertySymbol.Type, this.GenericContextForSymbolMapping );

    [Memo]
    public IMethod? GetMethod
        => this.PropertySymbol.GetMethod == null
            ? null
            : this.Compilation.Factory.GetMethod( this.PropertySymbol.GetMethod, this.GenericContextForSymbolMapping );

    [Memo]
    public virtual IMethod? SetMethod
        => this.PropertySymbol switch
        {
            // Generate a pseudo-setter for read-only automatic properties.
            { IsReadOnly: true } when this.PropertySymbol.IsAutoProperty() == true
                => new PseudoSetter( this, Accessibility.Private ),
            { SetMethod: null } => null,
            _ => this.Compilation.Factory.GetMethod( this.PropertySymbol.SetMethod, this.GenericContextForSymbolMapping )
        };

    public override MemberInfo ToMemberInfo() => this.ToPropertyInfo();

    public PropertyInfo ToPropertyInfo() => CompileTimePropertyInfo.Create( this );

    IRef<IPropertyOrIndexer> IPropertyOrIndexer.ToRef() => this.ToPropertyOrIndexerRef();

    IRef<IFieldOrPropertyOrIndexer> IFieldOrPropertyOrIndexer.ToRef() => this.ToFieldOrPropertyOrIndexerRef();

    protected abstract IRef<IPropertyOrIndexer> ToPropertyOrIndexerRef();

    protected abstract IRef<IFieldOrPropertyOrIndexer> ToFieldOrPropertyOrIndexerRef();

    public override string ToString() => this.PropertySymbol.ToString().AssertNotNull();

    [Memo]
    public Writeability Writeability
        => this.PropertySymbol switch
        {
            { IsReadOnly: true } when this.PropertySymbol.IsAutoProperty() == true => Writeability.ConstructorOnly,
            { IsReadOnly: true } => Writeability.None,
            { SetMethod: { IsInitOnly: true } _ } => Writeability.InitOnly,
            _ => Writeability.All
        };

    public override bool IsAsync => false;

    public IMethod? GetAccessor( MethodKind methodKind )
        => methodKind switch
        {
            MethodKind.PropertyGet => this.GetMethod,
            MethodKind.PropertySet => this.SetMethod,
            _ => null
        };

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
}