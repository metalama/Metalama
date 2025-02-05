// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
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

        var symbol = type.GetSingleMemberIncludingBase(
            this._templateMember.Name,
            symbol => classifier.IsTemplate( symbol )
                      && symbol.GetParameters().Select( p => p.Type ).SequenceEqual( parameters.Select( p => p.Type ), StructuralSymbolComparer.Default ) );

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