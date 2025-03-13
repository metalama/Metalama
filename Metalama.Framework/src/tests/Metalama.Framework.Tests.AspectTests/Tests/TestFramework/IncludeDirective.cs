// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(Imported\Imported.cs)
#endif
#pragma warning disable 169

using Metalama.Framework.Tests.AspectTests.Tests.TestFramework.Imported;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework
{
    [ImportedAspect]
    public class IncludeDirective { }
}