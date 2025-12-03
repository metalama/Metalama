// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_BasicProperty;

// <target>
[Observable]
public class FieldKeyword_BasicProperty
{
    // Semi-automatic property with basic field keyword usage.
    public string Name
    {
        get => field;
        set => field = value?.Trim() ?? throw new ArgumentNullException( nameof( value ) );
    } = "";
}
