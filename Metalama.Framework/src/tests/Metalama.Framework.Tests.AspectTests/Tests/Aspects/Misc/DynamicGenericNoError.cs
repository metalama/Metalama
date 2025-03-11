// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicGenericNoError;

// Test that dynamic type constructions are allowed in run-time code.

// <target>
class TargetCode
{
    Action<string, dynamic> dynamicGeneric;
    dynamic[] dynamicArray;
    (dynamic, int) dynamicTuple;
    ref dynamic DynamicRef => throw new Exception();

    Action<string, Func<dynamic, object>> dynamicConstructionGeneric;
    Func<dynamic, object>[] dynamicConstructionArray;
    (Func<dynamic, object>, int) dynamicConstructionTuple;
    ref Func<dynamic, object> DynamicConstructionRef => throw new Exception();
}