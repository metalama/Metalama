// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class TemplateAttributeFactory : IProjectService, IDisposable
{
    private readonly UserCodeAttributeDeserializer.Provider _attributeDeserializerProvider;

    private readonly ConcurrentDictionary<SerializableDeclarationId, IAdviceAttribute?> _cacheById = new();

    // We use a WeakCache here because when the service is used at design time, it can be used for several compilations,
    // and we don't want to prevent GC of symbols.
    private readonly WeakCache<ISymbol, IAdviceAttribute?> _cacheBySymbol = new();

    public TemplateAttributeFactory( in ProjectServiceProvider serviceProvider )
    {
        this._attributeDeserializerProvider = serviceProvider.GetRequiredService<UserCodeAttributeDeserializer.Provider>();
    }

    public bool TryGetTemplateAttribute(
        SerializableDeclarationId memberId,
        CompilationContext compilationContext,
        IDiagnosticAdder diagnosticAdder,
        [NotNullWhen( true )] out IAdviceAttribute? adviceAttribute )
    {
        if ( this._cacheById.TryGetValue( memberId, out adviceAttribute ) )
        {
            return adviceAttribute != null;
        }

        adviceAttribute =
            this._cacheById.GetOrAdd(
                memberId,
                static ( m, ctx ) =>
                {
                    _ = ctx.me.TryGetTemplateAttributeById( m, ctx.compilationContext, ctx.diagnosticAdder, out var attribute );

                    return attribute;
                },
                (me: this, compilationContext, diagnosticAdder) );

        return adviceAttribute != null;
    }

    private bool TryGetTemplateAttributeById(
        SerializableDeclarationId memberId,
        CompilationContext compilationContext,
        IDiagnosticAdder diagnosticAdder,
        out IAdviceAttribute? adviceAttribute )
    {
        var member = memberId.ResolveToSymbolOrNull( compilationContext );

        if ( member == null )
        {
            diagnosticAdder.Report( TemplatingDiagnosticDescriptors.CantResolveDeclaration.CreateRoslynDiagnostic( null, memberId.Id ) );

            adviceAttribute = null;

            return false;
        }

        if ( this._cacheBySymbol.TryGetValue( member, out adviceAttribute ) )
        {
            return adviceAttribute != null;
        }

        adviceAttribute = this._cacheBySymbol.GetOrAdd(
            member,
            m =>
            {
                _ = this.TryGetTemplateAttributeBySymbol( m, compilationContext, diagnosticAdder, out var attribute );

                return attribute;
            } );

        return adviceAttribute != null;
    }

    private static bool ImplementsAdviceAttributeInterface( INamedTypeSymbol type )
        => type.AllInterfaces.Any(
            i => i is { Name: nameof(IAdviceAttribute), ContainingNamespace: { } ns } && ns.GetFullName() == "Metalama.Framework.Advising" );

    private bool TryGetTemplateAttributeBySymbol(
        ISymbol member,
        CompilationContext compilationContext,
        IDiagnosticAdder diagnosticAdder,
        out IAdviceAttribute? adviceAttribute )
    {
        var attributeData = member
            .GetAttributes()
            .SingleOrDefault( a => a.AttributeClass != null && ImplementsAdviceAttributeInterface( a.AttributeClass ) );

        if ( attributeData == null )
        {
            // If we don't have a custom attribute here, we need to look at parent declarations.

            if ( member.IsOverride )
            {
                var overriddenMember = member switch
                {
                    IMethodSymbol method => (ISymbol?) method.OverriddenMethod,
                    IPropertySymbol property => property.OverriddenProperty,
                    IEventSymbol @event => @event.OverriddenEvent,
                    _ => throw new AssertionFailedException( $"Unexpected symbol: '{member}'." )
                };

                if ( overriddenMember != null )
                {
                    return this.TryGetTemplateAttributeBySymbol( overriddenMember, compilationContext, diagnosticAdder, out adviceAttribute );
                }
            }
            else if ( member is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } )
            {
                return this.TryGetTemplateAttributeBySymbol( associatedSymbol, compilationContext, diagnosticAdder, out adviceAttribute );
            }

            throw new AssertionFailedException( $"Cannot find the TemplateAttribute for '{member}'." );
        }

        var attributeDeserializer = this._attributeDeserializerProvider.Get( compilationContext );

        if ( !attributeDeserializer.TryCreateAttribute( attributeData, diagnosticAdder, out var attribute ) )
        {
            adviceAttribute = null;

            return false;
        }
        else
        {
            adviceAttribute = (IAdviceAttribute) attribute;

            return true;
        }
    }

    public void Dispose() => this._cacheBySymbol.Dispose();
}