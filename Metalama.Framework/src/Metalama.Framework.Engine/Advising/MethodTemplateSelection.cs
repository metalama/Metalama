// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using System;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Selects the template of a <see cref="MethodTemplateSelector"/> that applies to a target method, according to the async and iterator properties
/// of the target method.
/// </summary>
/// <remarks>
/// The selection is shared by the advice factory and by the methods that a pipeline extension declares from a template.
/// </remarks>
internal static class MethodTemplateSelection
{
    /// <summary>
    /// Selects the template of a <see cref="MethodTemplateSelector"/> that applies to a target method.
    /// </summary>
    /// <param name="templateClass">The class that contains the templates.</param>
    /// <param name="targetMethod">The method that the template implements.</param>
    /// <param name="templateSelector">The template names for each kind of method.</param>
    /// <returns>A reference to the selected template, interpreted as the template kind that the target method requires.</returns>
    public static TemplateMemberRef Select( TemplateClass templateClass, IMethod targetMethod, in MethodTemplateSelector templateSelector )
    {
        var defaultTemplate = ValidateTemplateName( templateClass, templateSelector.DefaultTemplate, TemplateKind.Default, true )!.Value;
        var asyncTemplate = ValidateTemplateName( templateClass, templateSelector.AsyncTemplate, TemplateKind.Async );

        var enumerableTemplate = ValidateTemplateName( templateClass, templateSelector.EnumerableTemplate, TemplateKind.IEnumerable );
        var enumeratorTemplate = ValidateTemplateName( templateClass, templateSelector.EnumeratorTemplate, TemplateKind.IEnumerator );

        var asyncEnumerableTemplate = ValidateTemplateName( templateClass, templateSelector.AsyncEnumerableTemplate, TemplateKind.IAsyncEnumerable, ignoreMissing: true );
        var asyncEnumeratorTemplate = ValidateTemplateName( templateClass, templateSelector.AsyncEnumeratorTemplate, TemplateKind.IAsyncEnumerator, ignoreMissing: true );

        var interpretedKind = TemplateKind.Default;

        var selectedTemplate = defaultTemplate;

        var asyncInfo = targetMethod.GetAsyncInfoImpl();
        var iteratorInfo = targetMethod.GetIteratorInfoImpl();

        // See if we have an async template, which actually does not need to have an async implementation, does not not need to 
        // be applied only on methods with async implementations. However, if the template has an async implementation, the
        // target awaitable type must be compatible with an async implementation, i.e. it must have a method builder.

        if ( asyncInfo.IsAsync == true || (templateSelector.UseAsyncTemplateForAnyAwaitable && (asyncInfo is { IsAwaitable: true, HasMethodBuilder: true } ||
                                                                                                iteratorInfo.EnumerableKind is EnumerableKind.IAsyncEnumerable
                                                                                                    or
                                                                                                    EnumerableKind.IAsyncEnumerator)) )
        {
            interpretedKind = TemplateKind.Async;

            if ( asyncTemplate.HasValue )
            {
                selectedTemplate = asyncTemplate.Value;

                // We don't return because the result can still be overwritten by async iterators.
            }
        }

        var useIteratorTemplate = iteratorInfo.IsIteratorMethod == true
                                  || (templateSelector.UseEnumerableTemplateForAnyEnumerable && iteratorInfo.EnumerableKind != EnumerableKind.None);

        switch ( iteratorInfo.EnumerableKind )
        {
            case EnumerableKind.None:
                break;

            case EnumerableKind.UntypedIEnumerable:
            case EnumerableKind.IEnumerable:
                if ( useIteratorTemplate && enumerableTemplate.HasValue )
                {
                    return enumerableTemplate.Value;
                }
                else
                {
                    interpretedKind = TemplateKind.IEnumerable;
                }

                break;

            case EnumerableKind.UntypedIEnumerator:
            case EnumerableKind.IEnumerator:
                if ( useIteratorTemplate && enumeratorTemplate.HasValue )
                {
                    return enumeratorTemplate.Value;
                }
                else
                {
                    interpretedKind = TemplateKind.IEnumerator;
                }

                break;

            case EnumerableKind.IAsyncEnumerable:
                if ( useIteratorTemplate && asyncEnumerableTemplate.HasValue )
                {
                    return asyncEnumerableTemplate.Value;
                }
                else
                {
                    interpretedKind = TemplateKind.IAsyncEnumerable;
                }

                break;

            case EnumerableKind.IAsyncEnumerator:
                if ( useIteratorTemplate && asyncEnumeratorTemplate.HasValue )
                {
                    return asyncEnumeratorTemplate.Value;
                }
                else
                {
                    interpretedKind = TemplateKind.IAsyncEnumerator;
                }

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return selectedTemplate.InterpretedAs( interpretedKind );
    }

    /// <summary>
    /// Resolves a template name of a selector, or returns <c>null</c> when the name is not given and not required.
    /// </summary>
    private static TemplateMemberRef? ValidateTemplateName(
        TemplateClass templateClass,
        string? templateName,
        TemplateKind templateKind,
        bool required = false,
        bool ignoreMissing = false )
    {
        if ( templateName == null )
        {
            if ( required )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(templateName),
                    $"A required template name was not provided for the template kind {templateKind}." );
            }

            return null;
        }

        return TemplateNameValidator.ValidateTemplateName( templateClass, templateName, templateKind, required, ignoreMissing );
    }
}
