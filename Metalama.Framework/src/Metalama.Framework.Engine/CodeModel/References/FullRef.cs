// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Specialization of <see cref="BaseRef{T}"/> for references bound to a <see cref="CompilationContext"/>.
/// </summary>
internal abstract partial class FullRef<T> : BaseRef<T>, IFullRef<T>
    where T : class, ICompilationElement
{
    protected FullRef( RefFactory refFactory )
    {
        this.RefFactory = refFactory;
    }

    public new IFullRef<TOut> As<TOut>()
        where TOut : class, ICompilationElement
        => this.CastAsFullRef<TOut>();

    protected abstract IFullRef<TOut> CastAsFullRef<TOut>()
        where TOut : class, ICompilationElement;

    protected sealed override IRef<TOut> CastAsRef<TOut>() => this.CastAsFullRef<TOut>();

    public sealed override bool IsDurable => false;

    protected CompilationContext CompilationContext => this.RefFactory.CompilationContext;

    IFullRef<T> IFullRef<T>.WithGenericContext( GenericContext genericContext ) => this.WithGenericContext( genericContext );

    public T Definition
    {
        get
        {
            if ( this.IsDefinition )
            {
                return this.ConstructedDeclaration;
            }
            else
            {
#if DEBUG
                using ( StackOverflowHelper.Detect() )
                {
#endif
                    return this.DefinitionRef.Definition;

#if DEBUG
                }
#endif
            }
        }
    }

    [Memo]
    public T ConstructedDeclaration => this.GetTarget( this.RefFactory.CanonicalCompilation );

    public abstract FullRef<T> WithGenericContext( GenericContext genericContext );

    public RefFactory RefFactory { get; }

    public ResolvedAttributeRef GetAttributes()
    {
        switch ( this.TargetKind )
        {
            case RefTargetKind.Return when this.GetSymbolIgnoringRefKind( this.CompilationContext ) is { Kind: SymbolKind.Method } and IMethodSymbol method:
                return new ResolvedAttributeRef( method.GetReturnTypeAttributes(), method, RefTargetKind.Return );

            case RefTargetKind.Field when this.GetSymbolIgnoringRefKind( this.CompilationContext ) is { Kind: SymbolKind.Event } and IEventSymbol @event:
                // Roslyn does not expose the backing field of an event, so we don't have access to its attributes.
                return new ResolvedAttributeRef( ImmutableArray<AttributeData>.Empty, @event, RefTargetKind.Field );

            case RefTargetKind.PrimaryConstructor when this.GetSymbolIgnoringRefKind( this.CompilationContext ) is { Kind: SymbolKind.NamedType } and INamedTypeSymbol namedType:
                var primaryConstructor = namedType.InstanceConstructors.FirstOrDefault( c => c.IsPrimaryConstructor() );

                if ( primaryConstructor != null )
                {
                    return new ResolvedAttributeRef( primaryConstructor.GetAttributes(), primaryConstructor, RefTargetKind.Default );
                }

                return new ResolvedAttributeRef( ImmutableArray<AttributeData>.Empty, namedType, RefTargetKind.PrimaryConstructor );

            default:
                var symbol = this.GetSymbol( this.CompilationContext, true );
                var attributes = symbol.GetAttributes();

                if ( symbol.Kind == SymbolKind.Assembly && symbol is IAssemblySymbol assemblySymbol )
                {
                    // Also return module-level attributes.
                    attributes = attributes.AddRange( assemblySymbol.Modules.SelectMany( m => m.GetAttributes() ) );
                }

                return new ResolvedAttributeRef( attributes, symbol, RefTargetKind.Default );
        }
    }

    public abstract bool IsDefinition { get; }

    public abstract IFullRef<T> DefinitionRef { get; }

    [Memo]
    private DeclarationIdRef<T> CompilationNeutralRef => new( this.ToSerializableId() );

    protected sealed override IDurableRef<T> ToDurable() => this.CompilationNeutralRef;

    public override SerializableDeclarationId ToSerializableId()
    {
        var symbol = this.GetSymbolIgnoringRefKind( this.RefFactory.CompilationContext );

        return symbol.GetSerializableId( this.TargetKind );
    }

    protected override ISymbol GetSymbol( CompilationContext compilationContext, bool ignoreAssemblyKey = false )
        => this.ApplyRefKind( this.GetSymbolIgnoringRefKind( compilationContext ) );

    protected abstract ISymbol GetSymbolIgnoringRefKind( CompilationContext compilationContext );

    public virtual ISymbol GetClosestContainingSymbol() => this.GetSymbolIgnoringRefKind( this.CompilationContext );

    public abstract SyntaxTree? PrimarySyntaxTree { get; }

    private ISymbol ApplyRefKind( ISymbol symbol )
        => (this.TargetKind, symbol.Kind) switch
        {
            (RefTargetKind.Assembly, SymbolKind.Assembly) when symbol is IAssemblySymbol => symbol,
            (RefTargetKind.Module, SymbolKind.NetModule) when symbol is IModuleSymbol => symbol,
            (RefTargetKind.NamedType or RefTargetKind.ExtensionBlock, SymbolKind.NamedType) when symbol is INamedTypeSymbol => symbol,
            (RefTargetKind.Default, _) => symbol,
            (RefTargetKind.Event, SymbolKind.Event) when symbol is IEventSymbol => symbol,
            (RefTargetKind.Property, SymbolKind.Property) when symbol is IPropertySymbol => symbol,
            (RefTargetKind.Field, SymbolKind.Field) when symbol is IFieldSymbol => symbol,
            (RefTargetKind.Return, _) => throw new InvalidOperationException( "Cannot get a symbol for the method return parameter." ),
            (RefTargetKind.Field, SymbolKind.Property) when symbol is IPropertySymbol property => property.GetBackingField().AssertSymbolNotNull(),
            (RefTargetKind.Field, SymbolKind.Event) when symbol is IEventSymbol => throw new InvalidOperationException( "Cannot get the underlying field of an event." ),
            (RefTargetKind.Parameter, SymbolKind.Property) when symbol is IPropertySymbol property => property.SetMethod.AssertSymbolNotNull().Parameters[0],
            (RefTargetKind.Parameter, SymbolKind.Method) when symbol is IMethodSymbol method => method.Parameters[0],
            (RefTargetKind.Property, SymbolKind.Parameter) when symbol is IParameterSymbol parameter => parameter.ContainingType.GetMembers( symbol.Name )
                .OfType<IPropertySymbol>()
                .Single(),
            (RefTargetKind.PrimaryConstructor, SymbolKind.NamedType) when symbol is INamedTypeSymbol namedType =>
                namedType.InstanceConstructors.FirstOrDefault( c => c.IsPrimaryConstructor() )
                ?? throw new AssertionFailedException( $"The type '{namedType}' does not have a primary constructor." ),
            _ => throw new AssertionFailedException( $"Don't know how to get the symbol kind {this.TargetKind} for a {symbol.Kind}." )
        };
}