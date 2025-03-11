// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestApiApplicationInfo : ApplicationInfoBase
{
    public TestApiApplicationInfo() : base( typeof(TestApiApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Testing.UnitTesting";

    public override bool ShouldCreateLocalCrashReports => false;
}