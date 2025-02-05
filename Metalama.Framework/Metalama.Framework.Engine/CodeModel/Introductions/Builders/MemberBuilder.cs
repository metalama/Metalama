// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal abstract class MemberBuilder : MemberOrNamedTypeBuilder, IMemberBuilder
{
    private bool _isVirtual;
    private bool _isAsync;
    private bool _isOverride;
    private bool _isExtern;

    protected MemberBuilder( INamedType declaringType, string name, AspectLayerInstance aspectLayerInstance ) :
        base( aspectLayerInstance, declaringType, name ) { }

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

    public abstract bool IsExplicitInterfaceImplementation { get; }

    public bool IsVirtual
    {
        get => this._isVirtual;
        set
        {
            this.CheckNotFrozen();

            this._isVirtual = value;
        }
    }

    public bool IsAsync
    {
        get => this._isAsync;
        set
        {
            this.CheckNotFrozen();

            this._isAsync = value;
        }
    }

    public bool IsOverride
    {
        get => this._isOverride;
        set
        {
            this.CheckNotFrozen();

            this._isOverride = value;
        }
    }

    public bool IsExtern
    {
        get => this._isExtern;
        set
        {
            this.CheckNotFrozen();

            this._isExtern = value;
        }
    }

    IMember IMember.Definition => this;

    IRef<IMember> IMember.ToRef() => this.ToMemberFullRef();

    protected abstract IFullRef<IMember> ToMemberFullRef();

    public bool HasImplementation => true;

    public override bool IsDesignTimeObservable => !this.IsOverride && !this.IsNew;

    public abstract IMember? OverriddenMember { get; }

    public override bool CanBeInherited => this.IsVirtual && !this.IsSealed && ((IDeclarationImpl) this.DeclaringType).CanBeInherited;
}