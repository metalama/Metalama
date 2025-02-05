// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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