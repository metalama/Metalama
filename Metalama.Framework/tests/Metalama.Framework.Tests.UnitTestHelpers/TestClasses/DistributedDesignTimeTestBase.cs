// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public class DistributedDesignTimeTestBase : UnitTestClass
{
    protected DistributedDesignTimeTestBase( ITestOutputHelper? logger = null ) : base( logger ) { }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );
    }

    protected override TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
    {
        if ( contextOptions.ProjectName != null )
        {
            throw new ArgumentOutOfRangeException();
        }

        return new DistributedDesignTimeTestContext( contextOptions, services );
    }

    [MustDisposeResource]
    protected DistributedDesignTimeTestContext CreateDistributedDesignTimeTestContext(
        ServiceProviderBuilder<IGlobalService>? userProcessServices = null,
        ServiceProviderBuilder<IGlobalService>? analysisProcessServices = null,
        TestContextOptions? options = null )
    {
        var services = new AdditionalServiceCollection();
        services.AddGlobalService( provider => new TestWorkspaceProvider( provider ) );
        services.AddGlobalService( provider => new TransformationPreviewServiceImpl( provider ) );
        services.AddGlobalService( provider => new CodeLensServiceImpl( provider ) );
        services.AddGlobalService( provider => new AspectDatabase( provider ) );

        services.AddUntypedGlobalService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );
        var context = (DistributedDesignTimeTestContext) this.CreateTestContext( options, services );
        _ = context.InitializeAsync( userProcessServices, analysisProcessServices );

        return context;
    }
}