// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes the template from which <see cref="CallSites.ExtensionTransformationFactory.DeclareMethod"/> generates the body of a method.
/// </summary>
/// <remarks>
/// <para>
/// A run-time parameter of the template binds to the parameter of the declared method that has the same name. Otherwise, it binds by its position
/// among the run-time parameters of the template to a parameter of the declared method that is neither one of the
/// <see cref="HiddenLeadingParameterCount"/> first parameters nor one of the <see cref="NameOnlyTrailingParameterCount"/> last parameters.
/// </para>
/// <para>
/// A run-time parameter of the template cannot have a default value or be a <c>params</c> parameter, and the template cannot have run-time type
/// parameters. Compile-time parameters and type parameters receive their values from <see cref="Arguments"/>.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class SynthesizedMethodTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedMethodTemplate"/> class.
    /// </summary>
    /// <param name="selector">The names of the templates for each kind of method. The template is selected according to the return type of the
    /// declared method, as for an override.</param>
    public SynthesizedMethodTemplate( in MethodTemplateSelector selector )
    {
        this.Selector = selector;
    }

    /// <summary>
    /// Gets the names of the templates for each kind of method.
    /// </summary>
    public MethodTemplateSelector Selector { get; }

    /// <summary>
    /// Gets the object that provides the template. The default is the default template provider of the origin of the request.
    /// </summary>
    public TemplateProvider TemplateProvider { get; init; }

    /// <summary>
    /// Gets the values of the compile-time parameters and type parameters of the template, in any form that advice arguments accept, or
    /// <c>null</c>.
    /// </summary>
    public object? Arguments { get; init; }

    /// <summary>
    /// Gets the tags that the template reads through <c>meta.Tags</c>, in any form that advice tags accept, or <c>null</c>.
    /// </summary>
    public object? Tags { get; init; }

    /// <summary>
    /// Gets the number of leading parameters of the declared method to which a run-time parameter of the template binds only by name. The receiver
    /// parameter of an interceptor is such a parameter.
    /// </summary>
    public int HiddenLeadingParameterCount { get; init; }

    /// <summary>
    /// Gets the number of trailing parameters of the declared method to which a run-time parameter of the template binds only by name. The
    /// parameters added to an interceptor are such parameters.
    /// </summary>
    public int NameOnlyTrailingParameterCount { get; init; }

    /// <summary>
    /// Gets the objects that the template reads through <c>meta.GetExtension</c>. Two objects cannot have the same type.
    /// </summary>
    public ImmutableArray<IMetaExtension> MetaExtensions { get; init; } = ImmutableArray<IMetaExtension>.Empty;
}
