// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Flashtrace.Loggers;
using Flashtrace.Records;
using Xunit.Abstractions;

namespace Metalama.Patterns.TestHelpers;

internal sealed class XUnitFlashtraceLogger : IFlashtraceRoleLoggerFactory
{
    private readonly FlashtraceRole _role;
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitFlashtraceLogger( FlashtraceRole role, ITestOutputHelper testOutputHelper )
    {
        this._role = role;
        this._testOutputHelper = testOutputHelper;
    }

    public IFlashtraceLogger GetLogger( Type type ) => new Logger( this._role, type.FullName!, this._testOutputHelper, this );

    public IFlashtraceLogger GetLogger( string sourceName ) => new Logger( this._role, sourceName, this._testOutputHelper, this );

    private sealed class Logger : SimpleFlashtraceLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public Logger( FlashtraceRole role, string name, ITestOutputHelper testOutputHelper, IFlashtraceRoleLoggerFactory factory ) : base( role, name )
        {
            this._testOutputHelper = testOutputHelper;
            this.Factory = factory;
        }

        public override bool IsEnabled( FlashtraceLevel level ) => true;

        public override IFlashtraceRoleLoggerFactory Factory { get; }

        protected override void Write( FlashtraceLevel level, LogRecordKind recordKind, string text, Exception? exception )
        {
            this._testOutputHelper.WriteLine( $"{level.ToString().ToUpperInvariant()} {this.Category}: {text}" );
        }
    }
}