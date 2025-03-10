// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.DynamicField;

#pragma warning disable CS0414

public class TheAspect : TypeAspect
{
    private dynamic f1 = null!;
    private dynamic? f2 = null;
    private dynamic[] f3 = null!;
    private List<dynamic> f4 = null!;
}