// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class TemplateClassMemberBuilder : ITemplateClassMemberBuilder
{
    private readonly ITemplateInfoService _symbolClassificationService;
    private readonly TemplateAttributeFactory _templateAttributeFactory;

    public TemplateClassMemberBuilder( ProjectServiceProvider serviceProvider )
    {
        this._symbolClassificationService = serviceProvider.GetRequiredService<ITemplateInfoService>();
        this._templateAttributeFactory = serviceProvider.GetRequiredService<TemplateAttributeFactory>();
    }

    public bool TryGetMembers(
        TemplateClass templateClass,
        INamedTypeSymbol type,
        CompilationContext compilationContext,
        IDiagnosticAdder diagnosticAdder,
        out ImmutableDictionary<string, TemplateClassMember> members )
    {
        var membersBuilder = ImmutableDictionary.CreateBuilder<string, TemplateClassMember>( StringComparer.Ordinal );
        var hasError = false;

        if ( templateClass.BaseClass != null )
        {
            foreach ( var baseMember in templateClass.BaseClass.Members )
            {
                var derivedMember = baseMember.Value with
                {
                    TemplateClass = templateClass,
                    Accessors = baseMember.Value.Accessors
                        .SelectAsReadOnlyCollection( kvp => (kvp.Key, Value: kvp.Value with { TemplateClass = templateClass }) )
                        .ToImmutableDictionary( kvp => kvp.Key, kvp => kvp.Value )
                };

                membersBuilder.Add( baseMember.Key, derivedMember );
            }
        }

        foreach ( var memberSymbol in type.GetMembers() )
        {
            var templateInfo = this._symbolClassificationService.GetTemplateInfo( memberSymbol );

            var memberKey = memberSymbol.Name;

            switch ( templateInfo.AttributeType )
            {
                case TemplateAttributeType.DeclarativeAdvice when memberSymbol is IMethodSymbol { AssociatedSymbol: not null }:
                    // This is an accessor of a template or event declarative advice. We don't index them.
                    continue;

                case TemplateAttributeType.DeclarativeAdvice:
                case TemplateAttributeType.InterfaceMember:
                    // For declarative advices and interface members, we don't require a unique name, so we identify the template by a special id.
                    memberKey = memberSymbol.GetDocumentationCommentId().AssertNotNull();

                    break;
            }

            IAdviceAttribute? attribute = null;

            if ( !templateInfo.IsNone && !this._templateAttributeFactory.TryGetTemplateAttribute(
                    templateInfo.Id,
                    compilationContext,
                    diagnosticAdder,
                    out attribute ) )
            {
                continue;
            }

            var templateParameters = ImmutableArray<TemplateClassMemberParameter>.Empty;
            var templateTypeParameters = ImmutableArray<TemplateClassMemberParameter>.Empty;
            var accessors = ImmutableDictionary<MethodKind, TemplateClassMember>.Empty;

            void AddAccessor( IMethodSymbol? accessor )
            {
                if ( accessor != null )
                {
                    var accessorParameters =
                        accessor.Parameters.Select( p => new TemplateClassMemberParameter( p, false, null ) )
                            .ToImmutableArray();

                    accessors = accessors.Add(
                        accessor.MethodKind,
                        new TemplateClassMember(
                            accessor.Name,
                            accessor.Name,
                            templateClass,
                            templateInfo,
                            attribute,
                            accessor.GetSerializableId(),
                            accessorParameters,
                            ImmutableArray<TemplateClassMemberParameter>.Empty,
                            ImmutableDictionary<MethodKind, TemplateClassMember>.Empty ) );
                }
            }

            switch ( memberSymbol )
            {
                case IMethodSymbol method:
                    {
                        // Forbid ref methods.
                        if ( method.RefKind != RefKind.None )
                        {
                            diagnosticAdder.Report(
                                GeneralDiagnosticDescriptors.RefMembersNotSupported.CreateRoslynDiagnostic(
                                    method.GetDiagnosticLocation(),
                                    method,
                                    templateClass ) );

                            hasError = true;
                        }

                        // Add parameters.
                        var parameterBuilder = ImmutableArray.CreateBuilder<TemplateClassMemberParameter>( method.Parameters.Length );
                        var allTemplateParametersCount = 0;

                        foreach ( var parameter in method.Parameters )
                        {
                            var isCompileTime = this._symbolClassificationService.IsCompileTimeParameter( parameter );

                            parameterBuilder.Add(
                                new TemplateClassMemberParameter(
                                    parameter,
                                    isCompileTime,
                                    allTemplateParametersCount ) );

                            allTemplateParametersCount++;
                        }

                        templateParameters = parameterBuilder.MoveToImmutable();

                        // Add type parameters.
                        var typeParameterBuilder = ImmutableArray.CreateBuilder<TemplateClassMemberParameter>( method.TypeParameters.Length );

                        foreach ( var typeParameter in method.TypeParameters )
                        {
                            var isCompileTime =
                                this._symbolClassificationService.IsCompileTimeTypeParameter( typeParameter );

                            typeParameterBuilder.Add(
                                new TemplateClassMemberParameter(
                                    typeParameter.Ordinal,
                                    typeParameter.Name,
                                    Type: null,
                                    isCompileTime,
                                    allTemplateParametersCount ) );

                            allTemplateParametersCount++;
                        }

                        templateTypeParameters = typeParameterBuilder.MoveToImmutable();

                        break;
                    }

                case IPropertySymbol property:
                    // Forbid ref properties.
                    if ( property.RefKind != RefKind.None )
                    {
                        diagnosticAdder.Report(
                            GeneralDiagnosticDescriptors.RefMembersNotSupported.CreateRoslynDiagnostic(
                                property.GetDiagnosticLocation(),
                                property,
                                templateClass ) );

                        hasError = true;
                    }

                    // Add accessors.
                    AddAccessor( property.GetMethod );
                    AddAccessor( property.SetMethod );

                    break;

                // ReSharper disable once UnusedVariable
                case IFieldSymbol field:
                    // Forbid ref fields.
                    if ( field.RefKind != RefKind.None )
                    {
                        diagnosticAdder.Report(
                            GeneralDiagnosticDescriptors.RefMembersNotSupported.CreateRoslynDiagnostic( field.GetDiagnosticLocation(), field, templateClass ) );

                        hasError = true;
                    }

                    break;

                case IEventSymbol @event:
                    AddAccessor( @event.AddMethod );
                    AddAccessor( @event.RemoveMethod );

                    break;
            }

            if ( memberSymbol is IMethodSymbol { MethodKind: MethodKind.PropertySet } && templateParameters.Length != 1 )
            {
                throw new AssertionFailedException( $"'{memberSymbol}' is a property setter but there is {templateParameters.Length} template parameters." );
            }

            var aspectClassMember = new TemplateClassMember(
                memberSymbol.Name,
                memberKey,
                templateClass,
                templateInfo,
                attribute,
                memberSymbol.GetSerializableId(),
                templateParameters,
                templateTypeParameters,
                accessors );

            if ( !templateInfo.IsNone )
            {
                if ( membersBuilder.TryGetValue( memberKey, out var existingMember ) && !memberSymbol.IsOverride &&
                     !existingMember.TemplateInfo.IsNone )
                {
                    // Note we cannot get here when the member is defined in the same type because the compile-time assembly creation
                    // would have failed.

                    // The template is already defined and we are not overwriting a template of the base class.
                    diagnosticAdder.Report(
                        GeneralDiagnosticDescriptors.TemplateWithSameNameAlreadyDefinedInBaseClass.CreateRoslynDiagnostic(
                            memberSymbol.GetDiagnosticLocation(),
                            (memberKey, type.Name, existingMember.TemplateClass.BaseClass!.Type.Name),
                            templateClass ) );

                    hasError = true;

                    continue;
                }

                // Add or replace the template.
                membersBuilder[memberKey] = aspectClassMember;
            }
            else
            {
                if ( !membersBuilder.ContainsKey( memberKey ) )
                {
                    membersBuilder.Add( memberKey, aspectClassMember );
                }
            }
        }

        members = membersBuilder.ToImmutable();

        return !hasError;
    }
}