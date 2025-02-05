// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestApiApplicationInfo : ApplicationInfoBase
{
    public TestApiApplicationInfo() : base( typeof(TestApiApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Testing.UnitTesting";

    public override bool ShouldCreateLocalCrashReports => false;
}