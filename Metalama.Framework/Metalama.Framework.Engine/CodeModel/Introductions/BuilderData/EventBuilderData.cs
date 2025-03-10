// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class EventBuilderData : MemberBuilderData
{
    private readonly IFullRef<IEvent> _ref;

    public ImmutableArray<IAttributeData> FieldAttributes { get; }

    public IRef<INamedType> Type { get; }

    public bool IsEventField { get; }

    public MethodBuilderData AddMethod { get; }

    public MethodBuilderData RemoveMethod { get; }

    public IRef<IEvent>? OverriddenEvent { get; }

    public IReadOnlyList<IRef<IEvent>> ExplicitInterfaceImplementations { get; }

    public IExpression? InitializerExpression { get; }

    public EventBuilderData( EventBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;

        Invariant.Assert( builder.RefKind == RefKind.None );

        this.FieldAttributes = builder.FieldAttributes.ToImmutableArray();
        this.Type = builder.Type.ToRef();
        this.AddMethod = new MethodBuilderData( builder.AddMethod, this._ref );
        this.RemoveMethod = new MethodBuilderData( builder.RemoveMethod, this._ref );
        this.OverriddenEvent = builder.OverriddenEvent?.ToRef();
        this.ExplicitInterfaceImplementations = builder.ExplicitInterfaceImplementations.SelectAsImmutableArray( i => i.ToRef() );
        this.IsEventField = builder.IsEventField;
        this.InitializerExpression = builder.InitializerExpression;
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<IEvent> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Event;

    public override IRef<IMember>? OverriddenMember => this.OverriddenEvent;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( [this.AddMethod, this.RemoveMethod] );
}