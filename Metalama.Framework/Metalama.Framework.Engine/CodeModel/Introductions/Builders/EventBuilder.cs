// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class EventBuilder : MemberBuilder, IEventBuilder, IEventImpl
{
    private readonly List<IAttributeData> _fieldAttributes;
    private INamedType _type;

    public IntroducedRef<IEvent> Ref { get; }

    public bool IsEventField { get; }

    public IReadOnlyList<IAttributeData> FieldAttributes => this._fieldAttributes;

    public EventBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType targetType,
        string name,
        bool isEventField )
        : base( targetType, name, aspectLayerInstance )
    {
        this.IsEventField = isEventField;
        this._type = (INamedType) targetType.Compilation.GetCompilationModel().Factory.GetTypeByReflectionType( typeof(EventHandler) );
        this._fieldAttributes = [];
        this.Ref = new IntroducedRef<IEvent>( this.Compilation.RefFactory );
    }

    public void AddFieldAttribute( IAttributeData attributeData ) => this._fieldAttributes.Add( attributeData );

    public INamedType Type
    {
        get => this._type;
        set => this._type = this.Translate( value );
    }

    public RefKind RefKind
    {
        get => RefKind.None;
        set => throw new NotSupportedException();
    }

    public IMethod Signature => this.Type.Methods.OfName( "Invoke" ).Single();

    [Memo]
    public AccessorBuilder AddMethod => new( this, MethodKind.EventAdd, this.IsEventField );

    [Memo]
    public AccessorBuilder RemoveMethod => new( this, MethodKind.EventRemove, this.IsEventField );

    IMethodBuilder IEventBuilder.AddMethod => this.AddMethod;

    IMethodBuilder IEventBuilder.RemoveMethod => this.RemoveMethod;

    public IMethodBuilder? RaiseMethod => null;

    public IEvent? OverriddenEvent { get; set; }

    public override DeclarationKind DeclarationKind => DeclarationKind.Event;

    IType IHasType.Type => this.Type;

    IType IHasTypeBuilder.Type
    {
        get => this.Type;

        set
        {
            switch ( value )
            {
                case INamedType namedType:
                    this.Type = this.Translate( namedType );

                    break;

                default:
                    throw new InvalidOperationException( "IEventBuilder.Type cannot be set to a value that does not implement INamedType." );
            }
        }
    }

    RefKind IHasType.RefKind => this.RefKind;

    IMethod IEvent.AddMethod => this.AddMethod;

    IMethod IEvent.RemoveMethod => this.RemoveMethod;

    IMethod? IEvent.RaiseMethod => this.RaiseMethod;

    // TODO: When an interface is introduced, explicit implementation should appear here.
    public IReadOnlyList<IEvent> ExplicitInterfaceImplementations { get; private set; } = Array.Empty<IEvent>();

    IEvent IEvent.Definition => this;

    public IExpression? InitializerExpression { get; set; }

    // public TemplateMember<IEvent>? InitializerTemplate { get; set; }

    public EventInfo ToEventInfo() => CompileTimeEventInfo.Create( this );

    public new IRef<IEvent> ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public IEventInvoker With( InvokerOptions options ) => new EventInvoker( this, options );

    public IEventInvoker With( object? target, InvokerOptions options = default ) => new EventInvoker( this, options, target );

    public object Add( object? handler ) => new EventInvoker( this ).Add( handler );

    public object Remove( object? handler ) => new EventInvoker( this ).Remove( handler );

    public object Raise( params object?[] args ) => new EventInvoker( this ).Raise( args );

    public void SetExplicitInterfaceImplementation( IEvent interfaceEvent ) => this.ExplicitInterfaceImplementations = [interfaceEvent];

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public override IMember? OverriddenMember => (IMemberImpl?) this.OverriddenEvent;

    public IMethod? GetAccessor( MethodKind methodKind ) => this.GetAccessorImpl( methodKind );

    public IEnumerable<IMethod> Accessors
    {
        get
        {
            yield return this.AddMethod;
            yield return this.RemoveMethod;

            if ( this.RaiseMethod != null )
            {
                yield return this.RaiseMethod;
            }
        }
    }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        this.AddMethod.Freeze();
        this.RemoveMethod.Freeze();
    }

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new EventBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public EventBuilderData BuilderData => (EventBuilderData) this.Ref.BuilderData;
}