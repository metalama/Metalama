// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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