// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that can be applied to fields or properties as custom attributes.
    /// </summary>
    /// <remarks>
    /// <para>This class is a redundant helper class. The aspect framework only respects the <see cref="IAspect{T}"/> interface.</para>
    /// </remarks>
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public abstract class FieldOrPropertyAspect : Aspect, IAspect<IFieldOrProperty>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IFieldOrProperty> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}