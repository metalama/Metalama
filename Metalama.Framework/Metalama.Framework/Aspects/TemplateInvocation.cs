// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Represents call to a template method.
/// </summary>
/// <param name="TemplateName">The name of the called template method.</param>
/// <param name="TemplateProvider">An optional <see cref="Metalama.Framework.Aspects.TemplateProvider"/>, or <c>default</c> if the
/// current template provider should be used.</param>
/// <param name="Arguments">Compile-time template arguments that will be passed to the template.</param>
[CompileTime]
public sealed record TemplateInvocation( string TemplateName, TemplateProvider TemplateProvider, object? Arguments = null )
{
    public TemplateInvocation( string templateName, object? arguments = null ) : this(
        templateName,
        default(TemplateProvider),
        arguments ) { }

    public TemplateInvocation( string templateName, ITemplateProvider templateProvider, object? arguments = null ) : this(
        templateName,
        TemplateProvider.FromInstance( templateProvider ),
        arguments ) { }
}