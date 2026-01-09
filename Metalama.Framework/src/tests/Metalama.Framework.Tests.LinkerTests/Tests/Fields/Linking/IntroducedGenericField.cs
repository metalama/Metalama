// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0169 // Field is never used

namespace Metalama.Framework.Tests.LinkerTests.Tests.Fields.Linking.IntroducedGenericField
{
    // <target>
    internal class Target
    {
        [PseudoIntroduction( "TestAspect" )]
        private global::System.Collections.Generic.Dictionary<global::System.Type, global::System.IDisposable> _configurations;
    }
}
