// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Loggers;
using JetBrains.Annotations;

namespace Flashtrace;

[Obsolete( "Use dependency injection." )]
[PublicAPI]
public static class FlashtraceSourceFactory
{
    public static IFlashtraceLoggerFactory DefaultFactory { get; set; } = NullFlashtraceLogger.Instance;

    public static IFlashtraceRoleLoggerFactory Default => DefaultFactory.ForRole( FlashtraceRole.Default );

    public static IFlashtraceRoleLoggerFactory ForRole( FlashtraceRole role ) => DefaultFactory.ForRole( role );
}