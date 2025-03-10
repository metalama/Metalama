// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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
            this.Parameter = parameter ?? throw new ArgumentNullException( nameof(parameter) );
        }

        public IParameter Parameter { get; }
    }
}