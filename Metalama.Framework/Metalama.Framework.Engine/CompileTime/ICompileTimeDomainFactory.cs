// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Gets an instance of <see cref="CompileTimeDomain"/>.
    /// </summary>
    [PublicAPI] // Used in Metalama.Try.
    public interface ICompileTimeDomainFactory : IGlobalService
    {
        /// <summary>
        /// Gets an instance of <see cref="CompileTimeDomain"/>. 
        /// </summary>
        CompileTimeDomain CreateDomain();
    }
}