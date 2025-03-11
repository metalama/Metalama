// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using PostSharp.Reflection;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="ContractAspect"/>.
    /// </summary>
    public interface ILocationValidationAspect : IAspect { }

    /// <summary>
    /// In Metalama, use <see cref="ContractAspect"/>.
    /// </summary>
    public interface ILocationValidationAspect<T> : ILocationValidationAspect
    {
        /// <summary>
        /// In Metalama, implement <see cref="ContractAspect.Validate"/>.
        /// </summary>
        Exception ValidateValue( T value, string locationName, LocationKind locationKind, LocationValidationContext context );
    }
}