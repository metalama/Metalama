// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Gives extensions information about the templates of a project.
/// </summary>
[PublicAPI]
public static class ExtensionTemplateServices
{
    /// <summary>
    /// Determines whether a template provider has a method template of a given name.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the project.</param>
    /// <param name="templateProvider">The template provider.</param>
    /// <param name="templateName">The name of the template.</param>
    /// <returns><c>true</c> when the type of the template provider is a template class of the project that declares a non-abstract method
    /// marked as a template with this name, and <c>false</c> otherwise.</returns>
    public static bool MethodTemplateExists( in ProjectServiceProvider serviceProvider, TemplateProvider templateProvider, string templateName )
    {
        if ( templateName == null )
        {
            throw new ArgumentNullException( nameof(templateName) );
        }

        if ( templateProvider.IsNull
             || !serviceProvider.GetRequiredService<TemplateClassProvider>().TryGet( templateProvider, out var templateClass )
             || !templateClass.Members.TryGetValue( templateName, out var member ) )
        {
            return false;
        }

        return member.TemplateInfo is { IsNone: false, IsAbstract: false } && member.DeclarationId.Id.StartsWith( "M:", StringComparison.Ordinal );
    }

    /// <summary>
    /// Gets the parameters of a method template, in the order of their declaration, with their types in a given compilation, followed by its type
    /// parameters.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the project.</param>
    /// <param name="compilation">The compilation in which the types of the parameters are returned.</param>
    /// <param name="templateProvider">The template provider.</param>
    /// <param name="templateName">The name of the template.</param>
    /// <param name="parameters">The parameters of the template, or a default array when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when <see cref="MethodTemplateExists"/> returns <c>true</c>, and <c>false</c> otherwise.</returns>
    /// <remarks>
    /// An extension uses this method to tell the compile-time parameters of a template, which receive template arguments, from its run-time
    /// parameters, which bind to the parameters of the method that the template implements.
    /// </remarks>
    public static bool TryGetMethodTemplateParameters(
        in ProjectServiceProvider serviceProvider,
        ICompilation compilation,
        TemplateProvider templateProvider,
        string templateName,
        [NotNullWhen( true )] out ImmutableArray<ExtensionTemplateParameter> parameters )
    {
        if ( compilation == null )
        {
            throw new ArgumentNullException( nameof(compilation) );
        }

        parameters = default;

        if ( !MethodTemplateExists( serviceProvider, templateProvider, templateName ) )
        {
            return false;
        }

        var compilationModel = (CompilationModel) compilation;
        var templateClass = serviceProvider.GetRequiredService<TemplateClassProvider>().Get( templateProvider );
        var member = templateClass.Members[templateName];

        var declaration = TemplateNameValidator.ValidateTemplateName( templateClass, templateName, TemplateKind.Default, true )!.Value
            .GetTemplateMember<IMethod>( compilationModel, serviceProvider, templateProvider, ObjectReader.Empty )
            .GetDeclaration( compilationModel );

        parameters = ImmutableArray.CreateRange(
                member.Parameters,
                p => new ExtensionTemplateParameter(
                    p.Name,
                    p.IsCompileTime,
                    declaration.Parameters[p.SourceIndex].Type,
                    declaration.Parameters[p.SourceIndex].RefKind ) )
            .AddRange(
                member.TypeParameters.Select(
                    p => new ExtensionTemplateParameter(
                        p.Name,
                        p.IsCompileTime,
                        declaration.TypeParameters[p.SourceIndex],
                        RefKind.None,
                        true ) ) );

        return true;
    }
}
