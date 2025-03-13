// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace MetaLamaTest;

public sealed class NotNullCheckAttribute : OverrideMethodAspect
{
    public override dynamic OverrideMethod()
    {
        foreach (var parameter in meta.Target.Parameters.Where(p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true))
        {
            ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);
        }

        return meta.Proceed();
    }

    public override Task<dynamic?> OverrideAsyncMethod()
    {
        foreach (var parameter in meta.Target.Parameters.Where(p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true))
        {
            ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);
        }

        return meta.ProceedAsync();
    }

    public override IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
    {
        foreach ( var parameter in meta.Target.Parameters.Where( p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true ) )
        {
            ArgumentNullException.ThrowIfNull( parameter.Value, parameter.Name );
        }

        return meta.ProceedAsyncEnumerable();
    }

    public override IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod()
    {
        foreach ( var parameter in meta.Target.Parameters.Where( p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true ) )
        {
            ArgumentNullException.ThrowIfNull( parameter.Value, parameter.Name );
        }

        return meta.ProceedAsyncEnumerator();
    }

    public override IEnumerable<dynamic?> OverrideEnumerableMethod()
    {
        foreach (var parameter in meta.Target.Parameters.Where(p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true))
        {
            ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);
        }

        return meta.ProceedEnumerable();
    }

    public override IEnumerator<dynamic?> OverrideEnumeratorMethod()
    {
        foreach (var parameter in meta.Target.Parameters.Where(p => p.RefKind is RefKind.None or RefKind.In
                                                                    && p.Type.IsNullable != true
                                                                    && p.Type.IsReferenceType == true))
        {
            ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);
        }

        return meta.ProceedEnumerator();
    }
}