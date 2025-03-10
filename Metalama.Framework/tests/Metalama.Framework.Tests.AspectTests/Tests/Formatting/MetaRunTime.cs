// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.MetaRunTime
{   
    class Aspect : IAspect
    {
        [Template]
        void Template()
        {
            var metalamaRelease = meta.RunTime( meta.CompileTime( new DateTime( 2023, 5, 3 ) ) );
            var now = meta.RunTime( DateTime.Now );
        }
    }
}