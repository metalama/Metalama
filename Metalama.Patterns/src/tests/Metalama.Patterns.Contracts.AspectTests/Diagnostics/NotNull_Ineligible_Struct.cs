// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class NotNull_Ineligible_Struct
{
    [NotNull]
    private DateTime field;
}