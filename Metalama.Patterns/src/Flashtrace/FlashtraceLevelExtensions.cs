// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Flashtrace;

[PublicAPI]
public static class FlashtraceLevelExtensions
{
    private const FlashtraceLevel _force = (FlashtraceLevel) 0x10;

    public static FlashtraceLevel WithForce( this FlashtraceLevel level ) => level | _force;

    public static FlashtraceLevel WithoutForce( this FlashtraceLevel level ) => level & ~_force;

    public static bool HasForce( this FlashtraceLevel level ) => (level & _force) != 0;

    public static FlashtraceLevel CopyForce( this FlashtraceLevel source, FlashtraceLevel other )
        => source.HasForce() ? other.WithForce() : other.WithoutForce();

    internal static LogLevel ToLogLevel( this FlashtraceLevel level )
        => level switch
        {
            FlashtraceLevel.Critical => LogLevel.Critical,
            FlashtraceLevel.Debug => LogLevel.Debug,
            FlashtraceLevel.Error => LogLevel.Error,
            FlashtraceLevel.Info => LogLevel.Information,
            FlashtraceLevel.None => LogLevel.None,
            FlashtraceLevel.Trace => LogLevel.Trace,
            FlashtraceLevel.Warning => LogLevel.Warning,
            _ => throw new ArgumentOutOfRangeException()
        };
}