// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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