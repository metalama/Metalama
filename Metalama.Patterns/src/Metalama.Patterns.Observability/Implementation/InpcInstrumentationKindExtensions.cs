// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal static class InpcInstrumentationKindExtensions
{
    public static bool? IsImplemented( this InpcInstrumentationKind kind )
        => kind switch
        {
            InpcInstrumentationKind.None => false,
            InpcInstrumentationKind.Unknown => null,
            _ => true
        };

    public static bool RequiresCast( this InpcInstrumentationKind kind ) => kind == InpcInstrumentationKind.InpcPrivateImplementation;
}