// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Net;

namespace Metalama.Patterns.Caching.TestHelpers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class CachingClassFixture
    {
        public EndPoint? Endpoint { get; set; }
    }
}