// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Assembly-level custom attribute that specifies the execution order of aspects or aspect layers.
    /// Define ordering relationships using this attribute to control how aspects compose with each other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Build-time vs run-time order:</b> Metalama follows the "matryoshka" model—aspects are applied from the inside out
    /// at build time, but executed from the outside in at runtime. The build-time order and run-time order are therefore opposite.
    /// Use <see cref="AspectOrderDirection.RunTime"/> to specify the more intuitive run-time order, or <see cref="AspectOrderDirection.CompileTime"/>
    /// for build-time order.
    /// </para>
    /// <para>
    /// <b>Partial ordering:</b> You can define partial order relationships using multiple attributes. Metalama merges all
    /// relationships from the current project and all referenced projects/libraries using topological sorting. When aspects
    /// have no explicit ordering relationship, alphabetical ordering is used for determinism.
    /// </para>
    /// <para>
    /// <b>Cross-project ordering:</b> Metalama automatically imports aspect order attributes from all referenced projects and assemblies.
    /// You do not need to repeat <c>[assembly: AspectOrder(...)]</c> attributes in projects that use aspects—it is sufficient
    /// to define them once in the projects that define the aspects.
    /// </para>
    /// <para>
    /// <b>Derived aspects:</b> By default (<see cref="ApplyToDerivedTypes"/> = <c>true</c>), ordering relationships apply
    /// to derived aspect classes as well. For example, if you order <c>CacheAspect</c> before <c>LoggingAspect</c>, this
    /// also orders <c>MemoryCacheAspect : CacheAspect</c> before <c>FileLoggingAspect : LoggingAspect</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Specify run-time order (more intuitive for users)
    /// [assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(CacheAspect), typeof(LoggingAspect), typeof(RetryAspect))]
    ///
    /// // Or specify build-time order (more intuitive for aspect authors)
    /// [assembly: AspectOrder(AspectOrderDirection.CompileTime, typeof(RetryAspect), typeof(LoggingAspect), typeof(CacheAspect))]
    ///
    /// // These two declarations are equivalent - they both result in the same run-time execution order
    /// </code>
    /// </example>
    /// <seealso cref="AspectOrderDirection"/>
    /// <seealso cref="LayersAttribute"/>
    /// <seealso href="@ordering-aspects"/>
    /// <seealso href="@multiple-instances"/>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    [PublicAPI]
    public sealed class AspectOrderAttribute : Attribute
    {
        public AspectOrderDirection Direction { get; }

        private readonly string[] _orderedAspectLayers;

        [Obsolete( "Explicitly specify AspectOrderDirection.RunTime for the 'direction' parameter." )]
        public AspectOrderAttribute( params Type[] orderedAspectTypes ) : this( AspectOrderDirection.RunTime, orderedAspectTypes ) { }

        [Obsolete( "Explicitly specify AspectOrderDirection.RunTime for the 'direction' parameter." )]
        public AspectOrderAttribute( params string[] orderedAspectLayers ) : this( AspectOrderDirection.RunTime, orderedAspectLayers ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectOrderAttribute"/> class that specifies the order of execution
        /// of aspects. This constructor does not allow multi-layer aspects to overlap each other. If aspects are composed
        /// of several layers, all layers of each aspect are ordered as a single group. To order layers individually, use
        /// the other constructor.
        /// </summary>
        /// <param name="direction">The direction in which the aspect types are supplied. <see cref="AspectOrderDirection.RunTime"/>
        /// means that the <paramref name="orderedAspectTypes"/> parameter specifies the run-time execution order, which is more intuitive to aspect users.
        /// <see cref="AspectOrderDirection.CompileTime"/> means that the compile-time execution order is supplied, which is intuitive to aspect authors.
        /// </param>
        /// <param name="orderedAspectTypes">A list of aspect types given the desired order of execution.</param>
        public AspectOrderAttribute( AspectOrderDirection direction, params Type[] orderedAspectTypes )
        {
            this.Direction = direction;
            this._orderedAspectLayers = orderedAspectTypes.Select( t => t.FullName + ":*" ).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectOrderAttribute"/> class that specified the order of execution
        /// of aspect layers. This constructor allows to specify the order of execution of individual layers.
        /// </summary>
        /// <param name="direction">The direction in which the aspect types are supplied. <see cref="AspectOrderDirection.RunTime"/>
        /// means that the <paramref name="orderedAspectLayers"/> parameter specifies the run-time execution order, which is more intuitive to aspect users.
        /// <see cref="AspectOrderDirection.CompileTime"/> means that the compile-time execution order is supplied, which is intuitive to aspect authors.
        /// </param>
        /// <param name="orderedAspectLayers">A list of layer names composed of the full name of the aspect type and the name
        /// of the aspect layer. The following formats are allowed: <c>MyNamespace.MyAspectType</c> to match the default layer,
        /// <c>MyNamespace.MyAspectType:MyLayer</c> to match a non-default layer, or <c>MyNamespace.MyAspectType:*</c> to match
        /// all layers of an aspect.
        /// </param>
        public AspectOrderAttribute( AspectOrderDirection direction, params string[] orderedAspectLayers )
        {
            this.Direction = direction;
            this._orderedAspectLayers = orderedAspectLayers;
        }

        /// <summary>
        /// Gets the ordered list of aspect layers, in the format specified the constructor documentation.
        /// </summary>
        public IReadOnlyList<string> OrderedAspectLayers => this._orderedAspectLayers;

        /// <summary>
        /// Gets or sets a value indicating whether the relationships should apply to derived aspect types. The default value
        /// is <c>true</c>.
        /// </summary>
        public bool ApplyToDerivedTypes { get; set; } = true;
    }
}