// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PseudoLayerOrder : Attribute
    {
        public PseudoLayerOrder(string aspectName, string? layerName = null)
        {
        }
    }
}
