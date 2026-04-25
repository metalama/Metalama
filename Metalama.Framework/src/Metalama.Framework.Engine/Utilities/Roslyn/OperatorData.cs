// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

#pragma warning disable SA1111, SA1113

internal sealed record OperatorData( OperatorKind Kind, string MemberName, LanguageVersion? MinimumLangVersion, SyntaxKind OperatorKeyword, bool IsChecked )
{
#pragma warning disable SA1115, SA1114
    public static ImmutableArray<OperatorData> All { get; } = ImmutableArray.Create(

        // Conversion operators - available since C# 1.0
        new OperatorData(
            OperatorKind.ImplicitConversion,
            WellKnownMemberNames.ImplicitConversionName,
            LanguageVersion.CSharp1,
            SyntaxKind.ImplicitKeyword,
            false ),
        new OperatorData(
            OperatorKind.ExplicitConversion,
            WellKnownMemberNames.ExplicitConversionName,
            LanguageVersion.CSharp1,
            SyntaxKind.ExplicitKeyword,
            false ),
        new OperatorData(
            OperatorKind.CheckedExplicitConversion,
            WellKnownMemberNames.CheckedExplicitConversionName,
            LanguageVersion.CSharp11,
            SyntaxKind.ExplicitKeyword,
            true ),

        // Arithmetic operators - available since C# 1.0
        new OperatorData( OperatorKind.Addition, WellKnownMemberNames.AdditionOperatorName, LanguageVersion.CSharp1, SyntaxKind.PlusToken, false ),
        new OperatorData( OperatorKind.Subtraction, WellKnownMemberNames.SubtractionOperatorName, LanguageVersion.CSharp1, SyntaxKind.MinusToken, false ),
        new OperatorData( OperatorKind.Multiplication, WellKnownMemberNames.MultiplyOperatorName, LanguageVersion.CSharp1, SyntaxKind.AsteriskToken, false ),
        new OperatorData( OperatorKind.Division, WellKnownMemberNames.DivisionOperatorName, LanguageVersion.CSharp1, SyntaxKind.SlashToken, false ),
        new OperatorData( OperatorKind.Modulus, WellKnownMemberNames.ModulusOperatorName, LanguageVersion.CSharp1, SyntaxKind.PercentToken, false ),
        new OperatorData( OperatorKind.UnaryNegation, WellKnownMemberNames.UnaryNegationOperatorName, LanguageVersion.CSharp1, SyntaxKind.MinusToken, false ),
        new OperatorData( OperatorKind.UnaryPlus, WellKnownMemberNames.UnaryPlusOperatorName, LanguageVersion.CSharp1, SyntaxKind.PlusToken, false ),
        new OperatorData( OperatorKind.Increment, WellKnownMemberNames.IncrementOperatorName, LanguageVersion.CSharp1, SyntaxKind.PlusPlusToken, false ),
        new OperatorData( OperatorKind.Decrement, WellKnownMemberNames.DecrementOperatorName, LanguageVersion.CSharp1, SyntaxKind.MinusMinusToken, false ),

        // Checked arithmetic operators - available since C# 11
        new OperatorData(
            OperatorKind.CheckedAddition,
            WellKnownMemberNames.CheckedAdditionOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.PlusToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedSubtraction,
            WellKnownMemberNames.CheckedSubtractionOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.MinusToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedMultiply,
            WellKnownMemberNames.CheckedMultiplyOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.AsteriskToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedDivision,
            WellKnownMemberNames.CheckedDivisionOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.SlashToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedUnaryNegation,
            WellKnownMemberNames.CheckedUnaryNegationOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.MinusToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedIncrement,
            WellKnownMemberNames.CheckedIncrementOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.PlusPlusToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedDecrement,
            WellKnownMemberNames.CheckedDecrementOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.MinusMinusToken,
            true ),

        // Bitwise operators - available since C# 1.0
        new OperatorData( OperatorKind.BitwiseAnd, WellKnownMemberNames.BitwiseAndOperatorName, LanguageVersion.CSharp1, SyntaxKind.AmpersandToken, false ),
        new OperatorData( OperatorKind.BitwiseOr, WellKnownMemberNames.BitwiseOrOperatorName, LanguageVersion.CSharp1, SyntaxKind.BarToken, false ),
        new OperatorData( OperatorKind.ExclusiveOr, WellKnownMemberNames.ExclusiveOrOperatorName, LanguageVersion.CSharp1, SyntaxKind.CaretToken, false ),
        new OperatorData( OperatorKind.OnesComplement, WellKnownMemberNames.OnesComplementOperatorName, LanguageVersion.CSharp1, SyntaxKind.TildeToken, false ),
        new OperatorData(
            OperatorKind.LeftShift,
            WellKnownMemberNames.LeftShiftOperatorName,
            LanguageVersion.CSharp1,
            SyntaxKind.LessThanLessThanToken,
            false ),
        new OperatorData(
            OperatorKind.RightShift,
            WellKnownMemberNames.RightShiftOperatorName,
            LanguageVersion.CSharp1,
            SyntaxKind.GreaterThanGreaterThanToken,
            false ),
        new OperatorData(
            OperatorKind.UnsignedRightShift,
            WellKnownMemberNames.UnsignedRightShiftOperatorName,
            LanguageVersion.CSharp11,
            SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken,
            false ),

        // Comparison operators - available since C# 1.0
        new OperatorData( OperatorKind.Equality, WellKnownMemberNames.EqualityOperatorName, LanguageVersion.CSharp1, SyntaxKind.EqualsEqualsToken, false ),
        new OperatorData(
            OperatorKind.Inequality,
            WellKnownMemberNames.InequalityOperatorName,
            LanguageVersion.CSharp1,
            SyntaxKind.ExclamationEqualsToken,
            false ),
        new OperatorData( OperatorKind.LessThan, WellKnownMemberNames.LessThanOperatorName, LanguageVersion.CSharp1, SyntaxKind.LessThanToken, false ),
        new OperatorData(
            OperatorKind.LessThanOrEqual,
            WellKnownMemberNames.LessThanOrEqualOperatorName,
            LanguageVersion.CSharp1,
            SyntaxKind.LessThanEqualsToken,
            false ),
        new OperatorData( OperatorKind.GreaterThan, WellKnownMemberNames.GreaterThanOperatorName, LanguageVersion.CSharp1, SyntaxKind.GreaterThanToken, false ),
        new OperatorData(
            OperatorKind.GreaterThanOrEqual,
            WellKnownMemberNames.GreaterThanOrEqualOperatorName,
            LanguageVersion.CSharp1,
            SyntaxKind.GreaterThanEqualsToken,
            false ),

        // Logical operators - True/False since C# 1.0, LogicalAnd/LogicalOr not user-definable
        new OperatorData( OperatorKind.LogicalNot, WellKnownMemberNames.LogicalNotOperatorName, LanguageVersion.CSharp1, SyntaxKind.ExclamationToken, false ),
        new OperatorData( OperatorKind.True, WellKnownMemberNames.TrueOperatorName, LanguageVersion.CSharp1, SyntaxKind.TrueKeyword, false ),
        new OperatorData( OperatorKind.False, WellKnownMemberNames.FalseOperatorName, LanguageVersion.CSharp1, SyntaxKind.FalseKeyword, false )

#if ROSLYN_5_0_0_OR_GREATER
       ,

        // Compound assignment operators - user-definable since C# 14
        new OperatorData(
            OperatorKind.AdditionAssignment,
            WellKnownMemberNames.AdditionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.PlusEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.SubtractionAssignment,
            WellKnownMemberNames.SubtractionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.MinusEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.MultiplicationAssignment,
            WellKnownMemberNames.MultiplicationAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.AsteriskEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.DivisionAssignment,
            WellKnownMemberNames.DivisionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.SlashEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.ModulusAssignment,
            WellKnownMemberNames.ModulusAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.PercentEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.BitwiseAndAssignment,
            WellKnownMemberNames.BitwiseAndAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.AmpersandEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.BitwiseOrAssignment,
            WellKnownMemberNames.BitwiseOrAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.BarEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.ExclusiveOrAssignment,
            WellKnownMemberNames.ExclusiveOrAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.CaretEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.LeftShiftAssignment,
            WellKnownMemberNames.LeftShiftAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.LessThanLessThanEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.RightShiftAssignment,
            WellKnownMemberNames.RightShiftAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.GreaterThanGreaterThanEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.UnsignedRightShiftAssignment,
            WellKnownMemberNames.UnsignedRightShiftAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken,
            false ),
        new OperatorData(
            OperatorKind.IncrementAssignment,
            WellKnownMemberNames.IncrementAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.PlusPlusToken,
            false ),
        new OperatorData(
            OperatorKind.DecrementAssignment,
            WellKnownMemberNames.DecrementAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.MinusMinusToken,
            false ),

        // Checked compound assignment operators - user-definable since C# 14
        new OperatorData(
            OperatorKind.CheckedAdditionAssignment,
            WellKnownMemberNames.CheckedAdditionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.PlusEqualsToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedSubtractionAssignment,
            WellKnownMemberNames.CheckedSubtractionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.MinusEqualsToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedMultiplicationAssignment,
            WellKnownMemberNames.CheckedMultiplicationAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.AsteriskEqualsToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedDivisionAssignment,
            WellKnownMemberNames.CheckedDivisionAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.SlashEqualsToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedIncrementAssignment,
            WellKnownMemberNames.CheckedIncrementAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.PlusPlusToken,
            true ),
        new OperatorData(
            OperatorKind.CheckedDecrementAssignment,
            WellKnownMemberNames.CheckedDecrementAssignmentOperatorName,
            LanguageVersion.CSharp14,
            SyntaxKind.MinusMinusToken,
            true )
#endif

    );

#pragma warning restore SA1115, SA1114

    private static readonly Dictionary<OperatorKind, OperatorData> _byKind = All.ToDictionary( x => x.Kind );

    private static readonly Dictionary<string, OperatorData> _byMemberName =
        All.Where( x => x.MinimumLangVersion != null ).ToDictionary( x => x.MemberName, x => x );

    public static OperatorData GetByKind( OperatorKind kind )
        => _byKind.TryGetValue( kind, out var data ) ? data : throw new ArgumentOutOfRangeException( nameof(kind) );

    public static OperatorData? GetByName( string methodName ) => _byMemberName.TryGetValue( methodName, out var data ) ? data : default;

    public static bool IsUserDefinable( OperatorKind kind ) => _byKind.ContainsKey( kind );

    public OperatorCategory Category { get; } = Kind.GetCategory();

    public bool IsStatic => this.Category is not (OperatorCategory.BinaryAssignment or OperatorCategory.UnaryAssignment);
}