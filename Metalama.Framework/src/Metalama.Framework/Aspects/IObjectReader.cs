// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Provides read-only dictionary access to tags passed from the <see cref="IAspect{T}.BuildAspect"/> method to templates.
    /// Accessible in templates via <see cref="meta.Tags"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tags are compile-time name-value pairs used to pass contextual information from <see cref="IAspect{T}.BuildAspect"/>
    /// to template implementations. This interface exposes tags as a dictionary while also providing access to the original
    /// source object(s) via the <see cref="Source"/> property.
    /// </para>
    /// <para>
    /// <b>Setting tags:</b> Tags can be set in two ways:
    /// </para>
    /// <list type="bullet">
    /// <item><description>By passing an argument to the <c>tags</c> parameter of advice methods (e.g., <c>builder.Override(nameof(Template), tags: new { Key = value })</c>)</description></item>
    /// <item><description>By setting <see cref="IAspectBuilder.Tags"/> at any point in <see cref="IAspect{T}.BuildAspect"/></description></item>
    /// </list>
    /// <para>
    /// When both methods are used, the tags are merged. In case of key conflicts, values from the advice method parameter take precedence.
    /// </para>
    /// <para>
    /// <b>Reading tags:</b> In template code, access tags through <c>meta.Tags</c> using two approaches:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Dictionary access:</b> Use <c>meta.Tags["key"]</c> to read individual values. This is the typical approach
    /// when using anonymous types (e.g., <c>new { Key = value }</c>).</description></item>
    /// <item><description><b>Source property:</b> Use <c>meta.Tags.Source</c> to access the original object cast to a strongly-typed class.
    /// This is useful when using a dedicated tag class instead of anonymous types.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="meta.Tags"/>
    /// <seealso cref="IAspectBuilder.Tags"/>
    /// <seealso href="@sharing-state-with-advice"/>
    [CompileTime]
    public interface IObjectReader : IReadOnlyDictionary<string, object?>
    {
        /// <summary>
        /// Gets the original source object(s) used to set the tags.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this property when you want to work with the original tag object rather than accessing individual keys.
        /// This is particularly useful when using anonymous types or custom tag classes, as it preserves the original type.
        /// </para>
        /// <para>
        /// If there are multiple source objects (i.e., when both <see cref="IAspectBuilder.Tags"/> and the
        /// <c>tags</c> advice method parameter are set), this property returns an <see cref="ImmutableArray{T}"/>
        /// containing all source objects.
        /// </para>
        /// </remarks>
        object? Source { get; }
    }
}