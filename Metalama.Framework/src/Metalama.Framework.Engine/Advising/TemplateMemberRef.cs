// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.Advising;

internal readonly struct TemplateMemberRef
{
    private readonly TemplateClassMember _templateMember;

    private readonly TemplateKind _selectedKind;

    private readonly TemplateKind _interpretedKind;

    private bool IsNull => this._selectedKind == TemplateKind.None;

    public TemplateMemberRef( TemplateClassMember template, TemplateKind selectedKind ) : this( template, selectedKind, selectedKind ) { }

    private TemplateMemberRef( TemplateClassMember template, TemplateKind selectedKind, TemplateKind interpretedKind )
    {
        this._templateMember = template;
        this._selectedKind = selectedKind;
        this._interpretedKind = interpretedKind;
    }

    public TemplateMember<T> GetTemplateMember<T>(
        CompilationModel compilation,
        in ProjectServiceProvider serviceProvider,
        in TemplateProvider templateProvider,
        IObjectReader tags )
        where T : class, IMemberOrNamedType
    {
        if ( this.IsNull )
        {
            throw new InvalidOperationException();
        }

        // TODO PERF: do not resolve dependencies here but upstream.
        var classifier = serviceProvider.GetRequiredService<SymbolClassificationService>();
        var templateAttributeFactory = serviceProvider.GetRequiredService<TemplateAttributeFactory>();

        var templateReflectionContext = this._templateMember.TemplateClass.GetTemplateReflectionContext( compilation.CompilationContext );
        var type = templateReflectionContext.Compilation.GetTypeByMetadataNameSafe( this._templateMember.TemplateClass.FullName );

        var parameters = this._templateMember.Parameters;
        var typeParameterCount = this._templateMember.TypeParameters.Length;

        // The parameter types are stored as durable identifiers, so that a template class, which lives as long as the
        // pipeline configuration, holds no symbol of the compilation it was built from. They are resolved here against
        // the compilation in which the template is being looked up, which is the one the comparison below is about.
        var templateReflectionCompilationContext = templateReflectionContext.Compilation.GetCompilationContext();
        var parameterTypes = parameters.SelectAsArray( p => p.GetTypeSymbol( templateReflectionCompilationContext ) );

        var symbol = type.GetSingleMemberIncludingBase(
            this._templateMember.Name,
            symbol => classifier.IsTemplate( symbol )
                      && symbol.GetParameters().Select( p => (ITypeSymbol?) p.Type ).SequenceEqual( parameterTypes, StructuralSymbolComparer.Default! )
                      && (symbol is not IMethodSymbol methodSymbol || methodSymbol.TypeParameters.Length == typeParameterCount) );

        var declaration = templateReflectionContext.GetCompilationModel( compilation ).Factory.GetDeclaration( symbol );

        if ( declaration is not T typedSymbol )
        {
            throw new InvalidOperationException( $"The template '{symbol}' is a {declaration.DeclarationKind} but it was expected to be an {typeof(T).Name}" );
        }

        // Create the attribute instance.

        if ( !templateAttributeFactory
                .TryGetTemplateAttribute(
                    this._templateMember.TemplateInfo.Id,
                    compilation.CompilationContext,
                    ThrowingDiagnosticAdder.Instance,
                    out var attribute ) )
        {
            throw new AssertionFailedException( $"Cannot instantiate the template attribute for '{symbol.ToDebugString()}'" );
        }

        if ( attribute is ITemplateAttribute templateAttribute )
        {
            return TemplateMemberFactory.Create(
                typedSymbol,
                this._templateMember,
                templateProvider,
                templateAttribute,
                tags,
                this._selectedKind,
                this._interpretedKind );
        }
        else
        {
            throw new AssertionFailedException( $"The attribute '{attribute.GetType().FullName}' does not implement ITemplateAttribute." );
        }
    }

    public TemplateMemberRef InterpretedAs( TemplateKind interpretedKind ) => new( this._templateMember, this._selectedKind, interpretedKind );

    public override string ToString() => this.IsNull ? "null" : $"{this._templateMember.Name}:{this._selectedKind}";
}