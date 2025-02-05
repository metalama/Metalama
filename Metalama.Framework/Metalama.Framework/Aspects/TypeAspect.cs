// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that can be applied to types as custom attributes.
    /// </summary>
    /// <remarks>
    /// <para>This class is a redundant helper class. The aspect framework only respects the <see cref="IAspect{T}"/> interface.</para>
    /// </remarks>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum )]
    [PublicAPI]
    public abstract class TypeAspect : Aspect, IAspect<INamedType>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            builder.MustBeRunTimeOnly();
        }
    }
}