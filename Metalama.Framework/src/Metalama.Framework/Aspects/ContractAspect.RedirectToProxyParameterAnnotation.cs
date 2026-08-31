// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.ComponentModel;

namespace Metalama.Framework.Aspects;

public abstract partial class ContractAspect
{
    /// <summary>
    /// This class supports Metalama framework infrastructure and should not be used directly by user code.
    /// </summary>
    [CompileTime]
    [EditorBrowsable( EditorBrowsableState.Never )]
    internal sealed class RedirectToProxyParameterAnnotation : IAnnotation<IFieldOrPropertyOrIndexer>, IAnnotation<IParameter>
    {
        public RedirectToProxyParameterAnnotation( IParameter parameter )
        {
            // The null check has to precede the conversion, which dereferences the compilation of its argument.
            if ( parameter == null )
            {
                throw new ArgumentNullException( nameof(parameter) );
            }

            this.Parameter = parameter.ToDurableRef();
        }

        /// <summary>
        /// Gets a durable reference to the proxy parameter, resolved against the compilation of the target where the
        /// annotation is consumed.
        /// </summary>
        /// <remarks>
        /// An annotation may be read in a compilation later than the one that produced it, so holding the
        /// <see cref="IParameter"/> itself would retain the earlier one.
        /// </remarks>
        public IDurableRef<IParameter> Parameter { get; }
    }
}