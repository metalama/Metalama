// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that can be applied the compilation as custom attributes (using the <c>[assembly: MyAspect]</c> syntax).
    /// </summary>
    /// <remarks>
    /// <para>This class is a redundant helper class. The aspect framework only respects the <see cref="IAspect{T}"/> interface.</para>
    /// </remarks>
    [AttributeUsage( AttributeTargets.Assembly )]
    [PublicAPI]
    public abstract class CompilationAspect : Aspect, IAspect<ICompilation>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<ICompilation> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<ICompilation> builder ) { }
    }
}