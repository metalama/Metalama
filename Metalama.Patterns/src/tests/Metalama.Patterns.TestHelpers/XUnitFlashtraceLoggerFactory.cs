// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using Xunit.Abstractions;

namespace Metalama.Patterns.TestHelpers;

public sealed class XUnitFlashtraceLoggerFactory : IFlashtraceLoggerFactory
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitFlashtraceLoggerFactory( ITestOutputHelper testOutputHelper )
    {
        this._testOutputHelper = testOutputHelper;
    }

    public IFlashtraceRoleLoggerFactory ForRole( FlashtraceRole role ) => new XUnitFlashtraceLogger( role, this._testOutputHelper );
}