// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Base class for compile-time entry points that execute within the compiler and IDE to add aspects, configure libraries,
    /// and implement architecture rules. This class cannot be inherited directly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fabrics are unique classes that execute at compile time and design time. Unlike aspects, fabrics do not need to be applied
    /// to any declaration or called from anywhere—they are invoked automatically simply because they exist in your code.
    /// Think of fabrics as compile-time entry points.
    /// </para>
    /// <para>
    /// To create a fabric, inherit from one of the following derived classes:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ProjectFabric"/> - Applies transformations to the current project.</description></item>
    /// <item><description><see cref="TransitiveProjectFabric"/> - Applies transformations to projects that reference the current assembly.</description></item>
    /// <item><description><see cref="NamespaceFabric"/> - Applies transformations to a specific namespace.</description></item>
    /// <item><description><see cref="TypeFabric"/> - Applies transformations to a specific type (implemented as a nested type).</description></item>
    /// </list>
    /// <para>
    /// With fabrics, you can add aspects programmatically using LINQ-like queries, configure aspect libraries, and implement architecture validation rules.
    /// </para>
    /// </remarks>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="TransitiveProjectFabric"/>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso cref="TypeFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@validation"/>
    [CompileTime]
    public abstract class Fabric : ICompileTimeSerializable, ITemplateProvider
    {
        private protected Fabric() { }
    }
}