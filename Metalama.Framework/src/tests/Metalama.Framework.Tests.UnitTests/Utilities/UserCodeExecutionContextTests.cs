// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests of what a <see cref="UserCodeExecutionContext"/> takes from the context that is current when it is created.
/// </summary>
public sealed class UserCodeExecutionContextTests : UnitTestClass
{
    private const string _code = """
                                 class Caller { }

                                 class Other { }
                                 """;

    /// <summary>
    /// Verifies that the ordinary factory fills in the target declaration from the ambient context, which is what an
    /// aspect relies on when it reports a diagnostic without naming a declaration.
    /// </summary>
    [Fact]
    public void CreateInstance_InheritsTheTargetDeclarationOfTheAmbientContext()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var caller = compilation.Types.OfName( "Caller" ).Single();

        var ambient = new UserCodeExecutionContext(
            testContext.ServiceProvider,
            UserCodeDescription.Create( "the ambient context" ),
            compilation,
            targetDeclaration: caller );

        using ( UserCodeExecutionContext.WithContext( ambient ) )
        {
            var created = UserCodeExecutionContext.CreateInstance(
                testContext.ServiceProvider,
                UserCodeDescription.Create( "the created context" ),
                compilation );

            Assert.Same( caller, created.TargetDeclaration );
        }
    }

    /// <summary>
    /// Verifies that the factory used by a static fabric amender takes nothing from the ambient context.
    /// </summary>
    /// <remarks>
    /// A static fabric amender builds a context on demand rather than storing one, so that the pipeline configuration
    /// does not pin a compilation (issue #1799). It is not running user code when it does so, and the queries it owns
    /// are reachable from user code through the public <c>IQuery.ToCollection</c>, so an ambient context at that moment
    /// belongs to the caller. Inheriting from it would give a fabric query the target declaration and the meta API of
    /// an unrelated aspect.
    /// </remarks>
    [Fact]
    public void CreateWithoutInheritance_TakesNothingFromTheAmbientContext()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var caller = compilation.Types.OfName( "Caller" ).Single();

        var ambient = new UserCodeExecutionContext(
            testContext.ServiceProvider,
            UserCodeDescription.Create( "the ambient context" ),
            compilation,
            targetDeclaration: caller );

        using ( UserCodeExecutionContext.WithContext( ambient ) )
        {
            var created = UserCodeExecutionContext.CreateWithoutInheritance(
                testContext.ServiceProvider,
                UserCodeDescription.Create( "the created context" ),
                compilation );

            Assert.Null( created.TargetDeclaration );
        }
    }

    /// <summary>
    /// Verifies that the ambient context is restored after a context has been created without inheritance, including
    /// when there was no ambient context to begin with.
    /// </summary>
    [Fact]
    public void CreateWithoutInheritance_RestoresTheAmbientContext()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Null( UserCodeExecutionContext.CurrentOrNull );

        _ = UserCodeExecutionContext.CreateWithoutInheritance(
            testContext.ServiceProvider,
            UserCodeDescription.Create( "the created context" ),
            compilation );

        Assert.Null( UserCodeExecutionContext.CurrentOrNull );

        var ambient = new UserCodeExecutionContext(
            testContext.ServiceProvider,
            UserCodeDescription.Create( "the ambient context" ),
            compilation );

        using ( UserCodeExecutionContext.WithContext( ambient ) )
        {
            _ = UserCodeExecutionContext.CreateWithoutInheritance(
                testContext.ServiceProvider,
                UserCodeDescription.Create( "the created context" ),
                compilation );

            Assert.Same( ambient, UserCodeExecutionContext.CurrentOrNull );
        }
    }
}
