// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.AspectWeavers
{
    /// <summary>
    /// Defines an aspect weaver that applies low-level transformations to Roslyn compilations using the Roslyn API directly.
    /// Aspect weavers bypass the standard <see cref="IAspect{T}.BuildAspect"/> method and provide full control over C# code transformations.
    /// Implementations must be public, have a default constructor, and be annotated with the <see cref="MetalamaPlugInAttribute"/> custom attribute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aspect weavers enable arbitrary C# code transformations that are not possible with the high-level <see cref="IAspectBuilder"/> advice API.
    /// They operate at the Roslyn syntax tree level and have direct access to the <see cref="Microsoft.CodeAnalysis.Compilation"/>.
    /// </para>
    /// <para>
    /// To create an aspect weaver:
    /// </para>
    /// <list type="number">
    /// <item><description>Reference the <c>Metalama.Framework.Sdk</c> package with <c>PrivateAssets="all"</c>.</description></item>
    /// <item><description>Create a class implementing <see cref="IAspectWeaver"/> and annotate it with <see cref="MetalamaPlugInAttribute"/>.</description></item>
    /// <item><description>Create an aspect class and annotate it with <see cref="RequireAspectWeaverAttribute"/> pointing to the weaver type.</description></item>
    /// <item><description>Implement the <see cref="TransformAsync"/> method to perform the transformations.</description></item>
    /// </list>
    /// <para>
    /// <b>Warning:</b> Weaver-based aspects are significantly more complex to implement, have worse IDE integration, and have
    /// a significant performance impact when many are in use. Prefer using the standard <see cref="Metalama.Framework.Aspects.IAspect{T}"/>
    /// approach when possible.
    /// </para>
    /// <para>
    /// Each weaver is invoked once per project, regardless of the number of aspect instances. The <see cref="AspectWeaverContext.AspectInstances"/>
    /// property provides the list of all aspect instances to process.
    /// </para>
    /// </remarks>
    /// <seealso cref="AspectWeaverContext"/>
    /// <seealso cref="MetalamaPlugInAttribute"/>
    /// <seealso cref="RequireAspectWeaverAttribute"/>
    /// <seealso cref="IAspectDriver"/>
    /// <seealso href="@aspect-weavers"/>
    [CompileTime]
    public interface IAspectWeaver : IAspectDriver
    {
        /// <summary>
        /// Transforms a Roslyn compilation according to the aspects being woven.
        /// </summary>
        /// <param name="context">The context providing access to the compilation, aspect instances, and weaving services.
        /// Use <see cref="AspectWeaverContext.AspectInstances"/> to iterate over targets, and set <see cref="AspectWeaverContext.Compilation"/>
        /// or use helper methods like <see cref="AspectWeaverContext.RewriteAspectTargetsAsync"/> to apply transformations.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        Task TransformAsync( AspectWeaverContext context );
    }
}