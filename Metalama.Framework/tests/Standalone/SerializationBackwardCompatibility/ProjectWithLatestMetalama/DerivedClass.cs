// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using ProjectWithMetalama20251;

namespace ProjectWithLatestMetalama;

public class DerivedClass : IInterface
{
#pragma warning disable CS0169 // Field is never used
    private SomeReferencedClass? _f;
#pragma warning restore CS0169 // Field is never used
}