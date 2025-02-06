// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.UnitTests.Assets;

public class StringLengthTestClass
{
    [StringLength( 5, 10 )]
    public string StringField;

    // Incorrect warning at build time, but no squiggly.
    // ReSharper disable once RedundantSuppressNullableWarningExpression

#pragma warning disable IDE0079 // Remove unnecessary suppression
    public string StringMethod( [StringLength( 10 )] string parameter ) => parameter!;
#pragma warning restore IDE0079 // Remove unnecessary suppression
}