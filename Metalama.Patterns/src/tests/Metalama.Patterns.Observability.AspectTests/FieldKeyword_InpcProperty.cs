// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Observability.AspectTests.Include;

#if TEST_OPTIONS
// @Include(Include/SimpleInpcByHand.cs)
#endif

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_InpcProperty;

// <target>
[Observable]
public class FieldKeyword_InpcProperty
{
    // Semi-automatic property with INPC type.
    public SimpleInpcByHand? Child
    {
        get => field;
        set => field = value ?? field;
    }

    // Computed property accessing the child's property.
    public int? ChildValue => this.Child?.A;
}
