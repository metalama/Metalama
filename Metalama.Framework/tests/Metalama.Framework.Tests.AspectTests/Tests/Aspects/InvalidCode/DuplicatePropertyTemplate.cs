// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.InvalidCode.DuplicatePropertyTemplate;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

class Aspect : TypeAspect
{
    [Template]
    public object Instance { get; set; }

#if TESTRUNNER
    [Template]
    public object Instance { get; set; }
#endif
}