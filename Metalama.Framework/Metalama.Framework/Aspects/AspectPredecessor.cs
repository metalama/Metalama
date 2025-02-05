// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Represents the relationship that an object (attribute, fabric, aspect) has created or required another aspect or validator.
    /// These relationships are exposed on <see cref="IAspectPredecessor.Predecessors"/>.
    /// </summary>
    [CompileTime]
    public readonly struct AspectPredecessor
    {
        /// <summary>
        /// Gets the kind of relationship represented by the current <see cref="AspectPredecessor"/>, and the kind of object
        /// present in the <see cref="Instance"/> property. 
        /// </summary>
        public AspectPredecessorKind Kind { get; }

        /// <summary>
        /// Gets the object that created the aspect instance. It can be an <see cref="IAspectInstance"/>, an <see cref="IFabricInstance"/>, or an <see cref="IAttribute"/>.
        /// </summary>
        public IAspectPredecessor Instance { get; }

        internal AspectPredecessor( AspectPredecessorKind kind, IAspectPredecessor instance )
        {
            this.Kind = kind;
            this.Instance = instance;
        }

        public override string ToString() => $"Kind={this.Kind}, Instance={{{this.Instance}}}";
    }
}