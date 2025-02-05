// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
            this.Parameter = parameter ?? throw new ArgumentNullException( nameof(parameter) );
        }

        public IParameter Parameter { get; }
    }
}