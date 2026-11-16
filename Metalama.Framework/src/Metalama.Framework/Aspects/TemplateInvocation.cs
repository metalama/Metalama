// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Represents a delegate-like encapsulation of a template method invocation. This enables passing template calls
/// as parameters to other templates, supporting advanced patterns like decorator composition.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TemplateInvocation"/> is similar to a delegate in that it captures everything needed to invoke a template:
/// the template name, the template provider, and any compile-time arguments. Unlike a delegate, it is a compile-time
/// construct that generates code when invoked via <see cref="meta.InvokeTemplate(TemplateInvocation, object?)"/>.
/// </para>
/// <para>
/// This is useful when an aspect allows customizations that must call back to the aspect's logic. For example, a caching
/// aspect could allow derived classes to wrap the caching logic in a try/catch by accepting a <see cref="TemplateInvocation"/>
/// that the customization template must invoke.
/// </para>
/// </remarks>
/// <param name="TemplateName">The name of the called template method. This method must be annotated with <see cref="TemplateAttribute"/>.</param>
/// <param name="TemplateProvider">An optional <see cref="Metalama.Framework.Aspects.TemplateProvider"/> specifying where to find the template,
/// or <c>default</c> if the current template provider (usually the current aspect) should be used.</param>
/// <param name="Arguments">Compile-time template arguments that will be passed to the template. These are typically anonymous objects
/// whose properties map to template parameters (e.g., <c>new { paramName = value }</c>).</param>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="ITemplateProvider"/>
/// <seealso cref="TemplateProvider"/>
/// <seealso cref="meta.InvokeTemplate(TemplateInvocation, object?)"/>
/// <seealso href="@templates"/>
/// <seealso href="@auxiliary-templates"/>
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