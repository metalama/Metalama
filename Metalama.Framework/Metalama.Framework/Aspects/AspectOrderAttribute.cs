// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Custom attribute that specifies the order  of execution of aspects or aspect layers.
    /// </summary>
    /// <seealso href="@ordering-aspects"/>
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