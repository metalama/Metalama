// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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