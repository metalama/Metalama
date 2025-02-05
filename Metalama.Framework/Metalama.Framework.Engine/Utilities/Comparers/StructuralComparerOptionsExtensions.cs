// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Utilities.Comparers;

internal static class StructuralComparerOptionsExtensions
{
    public static bool HasFlagFast( this StructuralComparerOptions options, StructuralComparerOptions flag ) => (options & flag) == flag;
}