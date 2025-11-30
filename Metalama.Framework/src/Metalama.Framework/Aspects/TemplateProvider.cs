// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Wraps a template provider type or instance for use in auxiliary template invocation and advice factory methods.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TemplateProvider"/> encapsulates either an <see cref="ITemplateProvider"/> instance or a type
/// that implements <see cref="ITemplateProvider"/>. This allows templates to be invoked from external classes
/// or from the current aspect.
/// </para>
/// <para>
/// Use <see cref="FromInstance"/> to wrap an existing template provider instance, or <see cref="FromType{T}"/>
/// to specify a template provider type. When passed to <see cref="meta.InvokeTemplate(string, TemplateProvider, object?)"/>
/// or <see cref="IAdviceFactory.WithTemplateProvider(ITemplateProvider)"/>, Metalama will look up template methods
/// from the specified provider.
/// </para>
/// <para>
/// Passing <c>default</c> (or a null provider) indicates that the current aspect should be used as the template provider.
/// </para>
/// </remarks>
/// <seealso cref="ITemplateProvider"/>
/// <seealso cref="TemplateInvocation"/>
/// <seealso cref="meta.InvokeTemplate(string, TemplateProvider, object?)"/>
/// <seealso cref="IAdviceFactory.WithTemplateProvider(ITemplateProvider)"/>
/// <seealso href="@templates"/>
/// <seealso href="@auxiliary-templates"/>
[CompileTime]
public readonly record struct TemplateProvider
{
    private readonly object? _value;

    private TemplateProvider( object? instance )
    {
        this._value = instance;
    }

    public bool IsNull => this._value == null;

    /// <summary>
    /// Creates a <see cref="TemplateProvider"/> from an object instance.
    /// </summary>
    public static TemplateProvider FromInstance( ITemplateProvider? instance )
    {
        return new TemplateProvider( instance );
    }

    public static TemplateProvider FromInstanceUnsafe( object? instance ) => new( instance );

    /// <summary>
    /// Creates a <see cref="TemplateProvider"/> from a type that implements <see cref="ITemplateProvider"/>.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="ITemplateProvider"/> and contains template methods
    /// (methods annotated with <see cref="TemplateAttribute"/>). The type must not be <c>static</c>; if you need
    /// a helper class with only static templates, make the class non-static and add a private constructor to prevent instantiation.</typeparam>
    /// <returns>A <see cref="TemplateProvider"/> that references the specified type.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when you want to invoke templates from a class that is not the current aspect or fabric.
    /// The type <typeparamref name="T"/> must implement <see cref="ITemplateProvider"/> (which is an empty marker interface)
    /// and contain one or more template methods.
    /// </para>
    /// <para>
    /// <b>Important:</b> The type <typeparamref name="T"/> is never instantiated. This means:
    /// <list type="bullet">
    /// <item><description>No constructor is required on <typeparamref name="T"/></description></item>
    /// <item><description>Instance members of <typeparamref name="T"/> are not accessible from the template</description></item>
    /// <item><description>Templates in <typeparamref name="T"/> must be <c>static</c> methods</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If you need to invoke instance templates or access instance state, use <see cref="FromInstance"/> instead,
    /// passing an actual instance of the template provider.
    /// </para>
    /// </remarks>
    /// <seealso href="@auxiliary-templates"/>
    public static TemplateProvider FromType<T>()
        where T : ITemplateProvider
        => FromTypeUnsafe( typeof(T) );

    internal static TemplateProvider FromTypeUnsafe( Type type )
    {
        return new TemplateProvider( type );
    }

    internal object? Object => this._value is Type ? null : this._value;

    internal Type? Type => this._value is Type type ? type : this._value?.GetType();
}