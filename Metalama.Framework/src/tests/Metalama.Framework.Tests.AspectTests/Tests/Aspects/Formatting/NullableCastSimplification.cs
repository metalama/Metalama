// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.NullableCastSimplification;

// Case 1: (string?)s returned as string? — the cast is redundant and should be simplified.
internal class ReturnAsNullableAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return (string?) meta.Target.Parameters[0].Value;
    }
}

// Case 2: (string?)s assigned to object? — the cast should still be simplified even though
// the surrounding conversion context targets object? (Copilot review feedback: use
// GetTypeInfo(node.Type).Type, not GetTypeInfo(node).ConvertedType).
internal class AssignToObjectAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        object? o = (string?) meta.Target.Parameters[0].Value;

        return o;
    }
}

// Case 3: (string?)s passed as argument to method expecting object? — same concern as case 2.
internal class PassToObjectParamAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return Helper.Consume( (string?) meta.Target.Parameters[0].Value );
    }
}

internal static class Helper
{
    public static object? Consume( object? value ) => value;
}

internal class TargetCode
{
    [ReturnAsNullableAspect]
    public static string? ReturnAsNullable( string s ) => s;

    [AssignToObjectAspect]
    public static object? AssignToObject( string s ) => s;

    [PassToObjectParamAspect]
    public static object? PassToObjectParam( string s ) => s;
}
