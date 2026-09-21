// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    /// <summary>
    /// The identifiers of the declarative advice members that did not resolve, and for which
    /// <see cref="GetDeclarativeAdvice(ProjectServiceProvider,CompilationContext,IDiagnosticAdder)"/> has therefore
    /// already reported a warning. The dictionary is used as a set, so the value of an entry carries no meaning.
    /// </summary>
    private readonly ConcurrentDictionary<SerializableDeclarationId, bool> _unresolvedDeclarativeAdvice = new();

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
        IObjectReader tags,
        IDiagnosticAdder diagnosticAdder )
    {
        var compilationModelForTemplateReflection = this._templateReflectionContext?.GetCompilationModel( compilation ) ?? compilation;

        return this.GetDeclarativeAdvice( serviceProvider, compilation.CompilationContext, diagnosticAdder )
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

    /// <summary>
    /// Returns the declarative advice members of the current class that resolve in the given compilation, ordered by
    /// <see cref="DeclarativeAdviceSymbolComparer"/>.
    /// </summary>
    /// <remarks>
    /// A member whose declaration identifier does not resolve is skipped, and the identifier is named by one
    /// <c>LAMA0295</c> warning. The identifier is written when the current class is created, and the current class is
    /// reached from the pipeline configuration, which is reused across compilations at design time, so the compilation
    /// an identifier is resolved against is not necessarily the one it was written from. The resolution therefore has
    /// to be allowed to fail: aborting here costs the project every aspect, every diagnostic and every suppression of
    /// the editor, which is what issue #2052 reports.
    /// </remarks>
    private IEnumerable<(TemplateClassMember TemplateClassMember, ISymbol Symbol, Compilation SymbolCompilation, DeclarativeAdviceAttribute Attribute)>
        GetDeclarativeAdvice(
            ProjectServiceProvider serviceProvider,
            CompilationContext compilationContext,
            IDiagnosticAdder diagnosticAdder )
    {
        TemplateAttributeFactory? templateAttributeFactory = null;

        var templateReflectionCompilationContext = this._templateReflectionContext?.CompilationContext ?? compilationContext;
        var templateReflectionCompilation = templateReflectionCompilationContext.Compilation;

        var resolvedMembers = new List<(TemplateClassMember TemplateClassMember, ISymbol Symbol, DeclarativeAdviceAttribute Attribute)>();

        foreach ( var member in this.Members.Values )
        {
            if ( member.TemplateInfo.AttributeType != TemplateAttributeType.DeclarativeAdvice )
            {
                continue;
            }

            var symbol = member.DeclarationId.ResolveToSymbolOrNull( templateReflectionCompilationContext );

            if ( symbol == null )
            {
                // The warning is reported at most once per identifier and per instance of the current class, because the
                // current class is asked for its declarative advice once per aspect instance, and reporting the same
                // warning once per target declaration would be of no use to the user.
                if ( this._unresolvedDeclarativeAdvice.TryAdd( member.DeclarationId, true ) )
                {
                    diagnosticAdder.Report(
                        TemplatingDiagnosticDescriptors.CantResolveDeclarativeAdvice.CreateRoslynDiagnostic(
                            null,
                            member.DeclarationId.Id,
                            this ) );
                }

                continue;
            }

            templateAttributeFactory ??= serviceProvider.GetRequiredService<TemplateAttributeFactory>();

            if ( !templateAttributeFactory.TryGetTemplateAttribute(
                    member.DeclarationId,
                    templateReflectionCompilationContext,
                    diagnosticAdder,
                    out var attribute ) )
            {
                continue;
            }

            resolvedMembers.Add( (member, symbol, (DeclarativeAdviceAttribute) attribute) );
        }

        // We are sorting the declarative advice by symbol name and not by source order because the source is not available
        // if the aspect library is a compiled assembly.
        return resolvedMembers
            .OrderBy( m => m.Symbol, DeclarativeAdviceSymbolComparer.Instance )
            .Select( m => (m.TemplateClassMember, m.Symbol, templateReflectionCompilation, m.Attribute) );
    }

    internal ITemplateReflectionContext GetTemplateReflectionContext( CompilationContext compilationContext )
        => this._templateReflectionContext ?? compilationContext;

    internal CompilationModel GetTemplateReflectionCompilation( CompilationModel compilationModel )
        => this._templateReflectionContext?.GetCompilationModel( compilationModel ) ?? compilationModel;

    string IDiagnosticSource.DiagnosticSourceDescription => this.ShortName;
}
