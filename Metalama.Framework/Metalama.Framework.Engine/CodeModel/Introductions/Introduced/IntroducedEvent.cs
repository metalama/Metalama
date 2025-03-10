// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedEvent : IntroducedMember, IEventImpl
{
    public EventBuilderData EventBuilderData { get; }

    public IntroducedEvent( EventBuilderData builderData, CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext )
    {
        this.EventBuilderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this.EventBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this.EventBuilderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this.EventBuilderData;

    protected override MemberBuilderData MemberBuilderData => this.EventBuilderData;

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    [Memo]
    public INamedType Type => this.MapDeclaration( this.EventBuilderData.Type );

    public IMethod Signature => this.Type.Methods.OfName( "Invoke" ).Single();

    [Memo]
    public IMethod AddMethod => new IntroducedAccessor( this, this.EventBuilderData.AddMethod );

    [Memo]
    public IMethod RemoveMethod => new IntroducedAccessor( this, this.EventBuilderData.RemoveMethod );

    public IMethod? RaiseMethod => null;

    [Memo]
    public IEvent? OverriddenEvent => this.MapDeclaration( this.EventBuilderData.OverriddenEvent );

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IEvent> ExplicitInterfaceImplementations
        => this.EventBuilderData.ExplicitInterfaceImplementations.SelectAsImmutableArray( this.MapDeclaration );

    [Memo]
    public IEvent Definition => this.Compilation.Factory.GetEvent( this.EventBuilderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    public EventInfo ToEventInfo() => CompileTimeEventInfo.Create( this );

    [Memo]
    private IFullRef<IEvent> Ref => this.RefFactory.FromIntroducedDeclaration<IEvent>( this );

    public IRef<IEvent> ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    public IExpression? InitializerExpression => this.EventBuilderData.InitializerExpression;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public IEventInvoker With( InvokerOptions options ) => new EventInvoker( this, options );

    public IEventInvoker With( object? target, InvokerOptions options = default ) => new EventInvoker( this, options, target );

    public object Add( object? handler ) => new EventInvoker( this ).Add( handler );

    public object Remove( object? handler ) => new EventInvoker( this ).Remove( handler );

    public object Raise( params object?[] args ) => new EventInvoker( this ).Raise( args );

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

    IType IHasType.Type => this.Type;

    RefKind IHasType.RefKind => RefKind.None;
}