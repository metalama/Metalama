// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Options;

public sealed class GetNullOptionsTests : UnitTestClass
{
    [Fact]
    public void GetOptionsWithoutProject()
    {
        // Test that GetOptions works (but returns a default value) without a project.

        using var context = this.CreateTestContext();
        var compilation = context.CreateCompilation( "class C;" );
        var type = compilation.Types.OfName( "C" ).Single();
        var option = type.Enhancements().GetOptions<Options>();
        Assert.NotNull( option );
    }

    private sealed class Options : IHierarchicalOptions<INamedType>
    {
        public object ApplyChanges( object changes, in ApplyChangesContext context ) => changes;

        public IHierarchicalOptions? GetDefaultOptions( OptionsInitializationContext context ) => null;
    }
}