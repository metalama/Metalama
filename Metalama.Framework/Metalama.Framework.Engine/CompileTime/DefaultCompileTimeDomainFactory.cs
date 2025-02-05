// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime
{
    [ExcludeFromCodeCoverage] // Not used in tests.
    internal sealed class DefaultCompileTimeDomainFactory : ICompileTimeDomainFactory
    {
        private readonly GlobalServiceProvider _serviceProvider;

        public DefaultCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
        }

        public CompileTimeDomain CreateDomain() => new( this._serviceProvider );
    }
}