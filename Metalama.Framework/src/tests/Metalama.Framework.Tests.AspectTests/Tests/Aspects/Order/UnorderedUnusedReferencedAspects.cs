// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequireOrderedAspects
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.UnorderedUnusedReferencedAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.UnorderedUnusedReferencedAspects;

public class Aspect1 : TypeAspect { }

public class Aspect2 : TypeAspect { }

// <target>
[Aspect1]
[Aspect2]
public class TargetClass { }
