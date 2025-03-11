// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Aspects.Configuration
{
    /// <summary>
    /// There is no aspect configuration in Metalama.
    /// </summary>
    [PublicAPI]
    public sealed class ManagedResourceIntroductionAspectConfiguration : AspectConfiguration
    {
        public ManagedResourceIntroductionAspectConfiguration( string name, byte[] data )
        {
            throw new NotImplementedException();
        }

        public ManagedResourceIntroductionAspectConfiguration( string name, Func<byte[]> dataProvider )
        {
            throw new NotImplementedException();
        }

        public string Name { get; }

        public byte[] Data { get; }

        public Func<byte[]> DataProvider { get; }
    }
}