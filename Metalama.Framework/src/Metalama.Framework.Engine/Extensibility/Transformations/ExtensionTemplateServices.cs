// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Services;
using System;

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
}
