// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Represents an invocation of a template method that can be kept across compilations. It is the durable counterpart of
/// <see cref="TemplateInvocation"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this type where an API keeps a template invocation after the compilation in which it was created, for instance a
/// registration that is evaluated again in each new compilation. Use <see cref="TemplateInvocation"/> everywhere else.
/// </para>
/// <para>
/// The values of <see cref="Arguments"/> must be durable. A declaration or a type must be passed as a durable reference
/// (see <see cref="RefExtensions.ToDurableRef{T}"/>), and an expression must be passed as a durable expression (see
/// <see cref="ExpressionExtensions.ToDurable"/>). Literals, enumeration values and <see cref="System.Type"/> values are
/// durable. A durable reference or a durable expression is resolved in the compilation in which the template is expanded,
/// so that the template receives the declaration, the type or the <see cref="IExpression"/> of that compilation.
/// </para>
/// </remarks>
/// <seealso cref="TemplateInvocation"/>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="TemplateProvider"/>
/// <seealso href="@templates"/>
[CompileTime]
[Durable]
public sealed class DurableTemplateInvocation
{
    /// <summary>
    /// The provider of the template: a <see cref="Type"/>, an <see cref="ITemplateProvider"/>, or <c>null</c> for the current
    /// template provider.
    /// </summary>
    /// <remarks>
    /// The provider is not stored as a <see cref="Metalama.Framework.Aspects.TemplateProvider"/>, because that type can wrap any object and
    /// is therefore not durable.
    /// </remarks>
    [Durable]
    private readonly object? _templateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTemplateInvocation"/> class for a template of the current template provider.
    /// </summary>
    /// <param name="templateName">The name of the called template method. This method must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="arguments">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.
    /// Its values must be durable.</param>
    public DurableTemplateInvocation( string templateName, [Durable] object? arguments = null )
    {
        this.TemplateName = templateName;
        this.Arguments = arguments;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTemplateInvocation"/> class for a template of a given <see cref="ITemplateProvider"/>.
    /// </summary>
    /// <param name="templateName">The name of the called template method. This method must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="templateProvider">The object that provides the template. It must be durable.</param>
    /// <param name="arguments">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.
    /// Its values must be durable.</param>
    public DurableTemplateInvocation( string templateName, [Durable] ITemplateProvider templateProvider, [Durable] object? arguments = null )
    {
        this.TemplateName = templateName;
        this._templateProvider = templateProvider;
        this.Arguments = arguments;
    }

    private DurableTemplateInvocation( string templateName, Type templateProviderType, [Durable] object? arguments )
    {
        this.TemplateName = templateName;
        this._templateProvider = templateProviderType;
        this.Arguments = arguments;
    }

    /// <summary>
    /// Creates a <see cref="DurableTemplateInvocation"/> for a static template declared by a given type.
    /// </summary>
    /// <typeparam name="T">The type that declares the template.</typeparam>
    /// <param name="templateName">The name of the called template method. This method must be static and annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="arguments">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.
    /// Its values must be durable.</param>
    /// <seealso cref="TemplateProvider.FromType{T}"/>
    public static DurableTemplateInvocation FromType<T>( string templateName, [Durable] object? arguments = null )
        where T : ITemplateProvider
        => new( templateName, typeof(T), arguments );

    /// <summary>
    /// Gets the name of the called template method.
    /// </summary>
    public string TemplateName { get; }

    /// <summary>
    /// Gets the <see cref="Metalama.Framework.Aspects.TemplateProvider"/> where the template is found, or <c>default</c> for the
    /// current template provider.
    /// </summary>
    public TemplateProvider TemplateProvider
        => this._templateProvider switch
        {
            Type type => TemplateProvider.FromTypeUnsafe( type ),
            ITemplateProvider instance => TemplateProvider.FromInstance( instance ),
            _ => default
        };

    /// <summary>
    /// Gets the object whose properties map to parameters or type parameters of the template, or <c>null</c>.
    /// </summary>
    [Durable]
    public object? Arguments { get; }
}
