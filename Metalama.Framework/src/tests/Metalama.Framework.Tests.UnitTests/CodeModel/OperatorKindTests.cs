// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="OperatorKind"/> and <see cref="OperatorKindExtensions"/>.
/// </summary>
public sealed class OperatorKindTests
{
    /// <summary>
    /// Verifies that <see cref="OperatorKind.NullCoalescingAssignment"/> was appended to the enumeration, so that the values of the
    /// existing members did not change.
    /// </summary>
    [Fact]
    public void NullCoalescingAssignment_IsLastMember()
        => Assert.Equal( OperatorKind.NullCoalescingAssignment, Enum.GetValues( typeof(OperatorKind) ).Cast<OperatorKind>().Max() );

    [Fact]
    public void NullCoalescingAssignment_GetCategory_IsBinaryAssignment()
        => Assert.Equal( OperatorCategory.BinaryAssignment, OperatorKind.NullCoalescingAssignment.GetCategory() );

    /// <summary>
    /// Verifies that <see cref="OperatorKindExtensions.GetCategory"/> handles every member of the enumeration, because it throws for an
    /// unknown member.
    /// </summary>
    [Fact]
    public void GetCategory_HandlesEveryMember()
    {
        foreach ( var kind in Enum.GetValues( typeof(OperatorKind) ).Cast<OperatorKind>() )
        {
            _ = kind.GetCategory();
        }
    }
}
