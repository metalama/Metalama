// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public abstract class DesignTimeTestBase : UnitTestClass
{
    protected DesignTimeTestBase( ITestOutputHelper? logger = null ) : base( logger ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService<IMetalamaProjectClassifier>( _ => new TestMetalamaProjectClassifier() );
        services.AddGlobalService<UserDiagnosticRegistrationService>( provider => new UserDiagnosticRegistrationService( provider ) );
    }
}