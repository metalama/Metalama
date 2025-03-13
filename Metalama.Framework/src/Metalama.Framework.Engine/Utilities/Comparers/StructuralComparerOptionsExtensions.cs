// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities.Comparers;

internal static class StructuralComparerOptionsExtensions
{
    public static bool HasFlagFast( this StructuralComparerOptions options, StructuralComparerOptions flag ) => (options & flag) == flag;
}