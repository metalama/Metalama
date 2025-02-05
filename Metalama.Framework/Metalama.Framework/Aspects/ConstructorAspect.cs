// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that can be applied to constructors as custom attributes.
    /// </summary>
    /// <remarks>
    /// <para>This class is a redundant helper class. The aspect framework only respects the <see cref="IAspect{T}"/> interface.</para>
    /// </remarks>
    [AttributeUsage( AttributeTargets.Constructor )]
    [PublicAPI]
    public abstract class ConstructorAspect : Aspect, IAspect<IConstructor>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IConstructor> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IConstructor> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}