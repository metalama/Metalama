// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_SetterSideEffect;

// <target>
[Observable]
public class FieldKeyword_SetterSideEffect
{
    // Semi-automatic property whose manual setter has a side effect (regression test for #1644).
    // The aspect must execute the original setter body, not just assign the raw value to the backing field.
    // The generated snapshot must route the public setter through 'Value_Source' so that 'SideEffect++' runs;
    // if the bug regresses, the setter assigns the backing field directly and the side effect is dropped.
    public int Value
    {
        get => field;
        set
        {
            field = value;
            this.SideEffect++;
        }
    }

    public int SideEffect { get; private set; }
}
