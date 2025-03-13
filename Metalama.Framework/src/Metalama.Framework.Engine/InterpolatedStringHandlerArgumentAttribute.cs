// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETSTANDARD2_0
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

[PublicAPI]
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute( string argument )
    {
        this.Arguments = [argument];
    }

    public InterpolatedStringHandlerArgumentAttribute( params string[] arguments )
    {
        this.Arguments = arguments;
    }

    public string[] Arguments { get; }
}
#endif