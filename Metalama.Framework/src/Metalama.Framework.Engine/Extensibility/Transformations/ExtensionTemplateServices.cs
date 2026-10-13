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

        return TryGetTemplateClass( serviceProvider, null, templateProvider, out var templateClass ) && IsMethodTemplate( templateClass, templateName );
    }

    /// <summary>
    /// Determines whether a template class declares a non-abstract method marked as a template with a given name.
    /// </summary>
    private static bool IsMethodTemplate( TemplateClass templateClass, string templateName )
        => templateClass.Members.TryGetValue( templateName, out var member )
           && member.TemplateInfo is { IsNone: false, IsAbstract: false }
           && member.DeclarationId.Id.StartsWith( "M:", StringComparison.Ordinal );

    /// <summary>
    /// Gets the template class of a template provider.
    /// </summary>
    /// <remarks>
    /// When the provider is the type fabric of <paramref name="origin"/>, the method returns the template class that the origin holds. Otherwise,
    /// it returns the template class of the project for the type of the provider, which includes project fabrics and namespace fabrics.
    /// </remarks>
    private static bool TryGetTemplateClass(
        in ProjectServiceProvider serviceProvider,
        ExtensionContributionOrigin? origin,
        TemplateProvider templateProvider,
        [NotNullWhen( true )] out TemplateClass? templateClass )
    {
        if ( templateProvider.IsNull )
        {
            templateClass = null;

            return false;
        }

        if ( origin?.TemplateClassInstance is { } templateClassInstance && templateClassInstance.TemplateProvider == templateProvider )
        {
            templateClass = templateClassInstance.TemplateClass;

            return true;
        }

        return serviceProvider.GetRequiredService<TemplateClassProvider>().TryGet( templateProvider, out templateClass );
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
        => TryGetMethodTemplateParameters( serviceProvider, compilation, null, templateProvider, templateName, out parameters );

    /// <summary>
    /// Gets the parameters of a method template, in the order of their declaration, with their types in a given compilation, followed by its type
    /// parameters. The template provider can be the fabric that made a contribution.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the project.</param>
    /// <param name="compilation">The compilation in which the types of the parameters are returned.</param>
    /// <param name="origin">The origin of the contribution that uses the template, or <c>null</c>.</param>
    /// <param name="templateProvider">The template provider.</param>
    /// <param name="templateName">The name of the template.</param>
    /// <param name="parameters">The parameters of the template, or a default array when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when the template provider is a template class of the project, or the type fabric of <paramref name="origin"/>, and
    /// declares a non-abstract method marked as a template with this name, and <c>false</c> otherwise.</returns>
    /// <remarks>
    /// The template class of a type fabric is not a template class of the project. Only the origin of a contribution made by the type fabric
    /// holds it. Use this overload when the template provider can be <see cref="ExtensionContributionOrigin.DefaultTemplateProvider"/>.
    /// </remarks>
    public static bool TryGetMethodTemplateParameters(
        in ProjectServiceProvider serviceProvider,
        ICompilation compilation,
        ExtensionContributionOrigin? origin,
        TemplateProvider templateProvider,
        string templateName,
        [NotNullWhen( true )] out ImmutableArray<ExtensionTemplateParameter> parameters )
    {
        if ( compilation == null )
        {
            throw new ArgumentNullException( nameof(compilation) );
        }

        if ( templateName == null )
        {
            throw new ArgumentNullException( nameof(templateName) );
        }

        parameters = default;

        if ( !TryGetTemplateClass( serviceProvider, origin, templateProvider, out var templateClass )
             || !IsMethodTemplate( templateClass, templateName ) )
        {
            return false;
        }

        var compilationModel = (CompilationModel) compilation;
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
