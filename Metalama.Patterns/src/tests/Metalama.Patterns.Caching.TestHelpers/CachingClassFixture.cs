// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Net;

namespace Metalama.Patterns.Caching.TestHelpers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class CachingClassFixture
    {
        public EndPoint? Endpoint { get; set; }
    }
}