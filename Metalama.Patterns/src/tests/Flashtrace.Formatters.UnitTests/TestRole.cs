// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests;

internal sealed class TestRole : FormattingRole
{
    public static readonly TestRole Instance = new();

    private TestRole() : base( nameof(TestRole) ) { }
}