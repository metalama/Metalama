// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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