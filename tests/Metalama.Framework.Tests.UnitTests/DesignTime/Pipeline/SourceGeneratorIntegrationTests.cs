// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Tests.UnitTests.DesignTime.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed class SourceGeneratorIntegrationTests : FrameworkBaseTestClass
{
    [Fact]
    public void ChangeInDependency_ChangeNotification()
    {
        var dependentProjectKey = ProjectKeyFactory.CreateTest( "Dependent" );
        var masterProjectKey = ProjectKeyFactory.CreateTest( "Master" );

        var mocks = new AdditionalServiceCollection();
        using var testContext = this.CreateTestContext( new TestContextOptions { HasSourceGeneratorTouchFile = true }, mocks );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var dependentCode = new Dictionary<string, string>()
        {
            ["dependent.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using System;

class MyAspect : TypeAspect
{
   public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
   {
        foreach ( var field in aspectBuilder.Target.BaseType.Fields )
        {
            aspectBuilder.Advice.IntroduceField( aspectBuilder.Target,  field.Name + ""Plus"", field.Type );
        }
   }
}

[MyAspect]
partial class C : BaseClass
{
   
}
"
        };

        // First compilation of everything.
        var masterCode1 = new Dictionary<string, string>() { ["master.cs"] = @"public class BaseClass { public int Field1; }" };

        var masterCompilation1 = TestCompilationFactory.CreateCSharpCompilation( masterCode1, name: masterProjectKey.AssemblyName );

        var dependentCompilation1 = TestCompilationFactory.CreateCSharpCompilation(
            dependentCode,
            name: dependentProjectKey.AssemblyName,
            additionalReferences: new[] { masterCompilation1.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, dependentCompilation1, TestableCancellationToken.None, out var results1 ) );

        BlockingCollection<ProjectKey> dirtyProjectNotifications = new();

        // ReSharper disable once AccessToDisposedClosure
        factory.EventHub.DirtyProject += project => dirtyProjectNotifications.Add( project, testContext.CancellationToken );

        Assert.Single( results1.Result.IntroducedSyntaxTrees );

        Assert.Contains(
            "Field1Plus",
            results1.Result.IntroducedSyntaxTrees.Single().Value.GeneratedSyntaxTree.ToString(),
            StringComparison.Ordinal );

        // Second compilation of the master compilation.
        var masterCode2 = new Dictionary<string, string>() { ["master.cs"] = @"public partial class BaseClass { public int Field2; }" };

        var masterCompilation2 = TestCompilationFactory.CreateCSharpCompilation( masterCode2, name: "Master" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, masterCompilation2, TestableCancellationToken.None, out _ ) );

        var notification = dirtyProjectNotifications.Take();

        Assert.Equal( dependentProjectKey.AssemblyName, notification.AssemblyName );
    }
}