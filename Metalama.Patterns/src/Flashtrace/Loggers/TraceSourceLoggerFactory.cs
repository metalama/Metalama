// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Loggers;

[PublicAPI]
public sealed class TraceSourceLoggerFactory : IFlashtraceLoggerFactory
{
    IFlashtraceRoleLoggerFactory IFlashtraceLoggerFactory.ForRole( FlashtraceRole role ) => new RoleLoggerFactory( role );

    private sealed class RoleLoggerFactory : IFlashtraceRoleLoggerFactory
    {
        private readonly FlashtraceRole _role;

        public RoleLoggerFactory( FlashtraceRole role )
        {
            this._role = role;
        }

        IFlashtraceLogger IFlashtraceRoleLoggerFactory.GetLogger( Type type ) => new TraceSourceFlashtraceLogger( this, this._role, type.FullName! );

        public IFlashtraceLogger GetLogger( string sourceName ) => new TraceSourceFlashtraceLogger( this, this._role, sourceName );
    }
}