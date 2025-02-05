// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// A base class for <see cref="AspectClass"/> and <see cref="FabricTemplateClass"/>. Represents an aspect, but does not
/// assume the class implements the <see cref="IAspect"/> semantic.
/// </summary>
public abstract class TemplateClass : IDiagnosticSource
{
    protected ProjectServiceProvider ServiceProvider { get; }

    private readonly ConcurrentDictionary<string, TemplateDriver> _templateDrivers = new( StringComparer.Ordinal );
    private readonly ITemplateReflectionContext? _templateReflectionContext; // TODO: Don't keep a reference because this goes to the pipeline config. But how?

    internal TemplateClass? BaseClass { get; }

    private protected TemplateClass(
        ProjectServiceProvider serviceProvider,
        ITemplateReflectionContext templateReflectionContext,
        INamedTypeSymbol typeSymbol,
        IDiagnosticAdder diagnosticAdder,
        TemplateClass? baseClass,
        string shortName )
    {
        var memberBuilder = serviceProvider.GetRequiredService<ITemplateClassMemberBuilder>();
        
        this.ServiceProvider = serviceProvider;
        this.BaseClass = baseClass;

        this.ShortName = shortName;

        if ( templateReflectionContext.IsCacheable )
        {
            this._templateReflectionContext = templateReflectionContext;
        }

        // This condition is to work around fakes.
        if ( !typeSymbol.GetType().Assembly.IsDynamic )
        {
            this.TypeId = typeSymbol.GetSerializableTypeId();
        }
        else
        {
            // We have a fake!!
            this.TypeId = default;
        }
        
        this.HasError = !memberBuilder.TryGetMembers( this, typeSymbol, templateReflectionContext.CompilationContext, diagnosticAdder, out var members );
        this.Members = members;
    }

    public string ShortName { get; }

    internal ImmutableDictionary<string, TemplateClassMember> Members { get; }

    protected bool HasError { get; set; }

    public SerializableTypeId TypeId { get; }

    /// <summary>
    /// Gets the reflection type for the current <see cref="TemplateClass"/>.
    /// </summary>
    internal abstract Type Type { get; }

    internal TemplateDriver GetTemplateDriver( IRef sourceTemplate )
    {
        var templateSymbol = ((ISymbolRef) sourceTemplate).Symbol;
        var id = templateSymbol.GetDocumentationCommentId()!;

        if ( this._templateDrivers.TryGetValue( id, out var templateDriver ) )
        {
            return templateDriver;
        }

        var compiledTemplateMethodInfo = this.GetCompiledTemplateMethodInfo( templateSymbol );

        templateDriver = new TemplateDriver( this.ServiceProvider, compiledTemplateMethodInfo );

        if ( this._templateDrivers.TryAdd( id, templateDriver ) )
        {
            return templateDriver;
        }
        else
        {
            // Another thread instantiated the same driver in the meantime.
            return this._templateDrivers[id];
        }
    }

    internal MethodInfo GetCompiledTemplateMethodInfo( ISymbol templateSymbol )
    {
        var templateName = TemplateNameHelper.GetCompiledTemplateName( templateSymbol );

        return this.Type.GetAnyMethod( templateName )
               ?? throw new AssertionFailedException( $"Could not find the compile template for {templateSymbol}." );
    }

    public abstract string FullName { get; }

    internal bool TryGetInterfaceMember( ISymbol symbol, [NotNullWhen( true )] out TemplateClassMember? member )
        => this.Members.TryGetValue( symbol.GetDocumentationCommentId().AssertNotNull(), out member )
           && member.TemplateInfo.AttributeType == TemplateAttributeType.InterfaceMember;

    internal IEnumerable<TemplateMember<IMemberOrNamedType>> GetDeclarativeAdvice(
        in ProjectServiceProvider serviceProvider,
        CompilationModel compilation,
        TemplateProvider templateProvider,
        IObjectReader tags )
    {
        var compilationModelForTemplateReflection = this._templateReflectionContext?.GetCompilationModel( compilation ) ?? compilation;

        return this.GetDeclarativeAdvice( serviceProvider, compilation.CompilationContext )
            .Select(
                x => TemplateMemberFactory.Create(
                    (IMemberOrNamedType) compilationModelForTemplateReflection.Factory.GetDeclaration(
                        compilationModelForTemplateReflection.CompilationContext.SymbolTranslator.Translate( x.Symbol, x.SymbolCompilation )
                            .AssertNotNull() ),
                    x.TemplateClassMember,
                    templateProvider,
                    x.Attribute,
                    tags ) );
    }

    private IEnumerable<(TemplateClassMember TemplateClassMember, ISymbol Symbol, Compilation SymbolCompilation, DeclarativeAdviceAttribute Attribute)>
        GetDeclarativeAdvice(
            ProjectServiceProvider serviceProvider,
            CompilationContext compilationContext )
    {
        TemplateAttributeFactory? templateAttributeFactory = null;

        var templateReflectionCompilationContext = this._templateReflectionContext?.CompilationContext ?? compilationContext;
        var templateReflectionCompilation = templateReflectionCompilationContext.Compilation;

        // We are sorting the declarative advice by symbol name and not by source order because the source is not available
        // if the aspect library is a compiled assembly.

        return this.Members
            .Where( m => m.Value.TemplateInfo.AttributeType == TemplateAttributeType.DeclarativeAdvice )
            .Select(
                m =>
                {
                    var symbol = m.Value.DeclarationId.ResolveToSymbol( templateReflectionCompilationContext );

                    return (Template: m.Value, Symbol: symbol, Syntax: symbol.GetPrimarySyntaxReference());
                } )
            .OrderBy( m => m.Symbol, DeclarativeAdviceSymbolComparer.Instance )
            .Select( m => (m.Template, m.Symbol, templateReflectionCompilation, ResolveAttribute( m.Template.DeclarationId )) );

        DeclarativeAdviceAttribute ResolveAttribute( SerializableDeclarationId declarationId )
        {
            templateAttributeFactory ??= serviceProvider.GetRequiredService<TemplateAttributeFactory>();

            if ( !templateAttributeFactory.TryGetTemplateAttribute(
                    declarationId,
                    templateReflectionCompilationContext,
                    ThrowingDiagnosticAdder.Instance,
                    out var attribute ) )
            {
                throw new AssertionFailedException( $"Cannot get a template for '{declarationId}'." );
            }

            return (DeclarativeAdviceAttribute) attribute;
        }
    }

    internal ITemplateReflectionContext GetTemplateReflectionContext( CompilationContext compilationContext )
        => this._templateReflectionContext ?? compilationContext;

    internal CompilationModel GetTemplateReflectionCompilation( CompilationModel compilationModel )
        => this._templateReflectionContext?.GetCompilationModel( compilationModel ) ?? compilationModel;

    string IDiagnosticSource.DiagnosticSourceDescription => this.ShortName;
}