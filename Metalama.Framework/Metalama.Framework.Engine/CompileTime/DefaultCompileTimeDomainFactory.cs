// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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