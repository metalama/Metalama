// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This file intentionally has no Metalama using directive, so the templating validator checks it in its second phase.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug2171_Inherited;

// <target>
[Introduction( IntroducedProperty = 42 )]
internal class TargetClass { }
