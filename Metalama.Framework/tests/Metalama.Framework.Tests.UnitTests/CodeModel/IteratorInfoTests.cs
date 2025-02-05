// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class IteratorInfoTests : UnitTestClass
{
    protected override void ConfigureServices( IAdditionalServiceCollection services ) => services.AddProjectService( SyntaxGenerationOptions.Formatted );
    
    [Fact]
    public void GenericEnumerableYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.True( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IEnumerable, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }

    [Fact]
    public void GenericEnumeratorYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
    IEnumerator<int> Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.True( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IEnumerator, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void AsyncEnumerableYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
async IAsyncEnumerable<int> Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.True( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IAsyncEnumerable, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }

    [Fact]
    public void AsyncEnumeratorYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
async IAsyncEnumerator<int> Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.True( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IAsyncEnumerator, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }
#endif

    [Fact]
    public void NonGenericEnumeratorYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections;
class C
{
    IEnumerator Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

            Assert.True( iteratorInfo.IsIteratorMethod );
            Assert.Equal( EnumerableKind.UntypedIEnumerator, iteratorInfo.EnumerableKind );
            Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(object) ), iteratorInfo.ItemType );
        }
    }

    [Fact]
    public void NonGenericEnumerableYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections;
class C
{
    IEnumerable Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

            Assert.True( iteratorInfo.IsIteratorMethod );
            Assert.Equal( EnumerableKind.UntypedIEnumerable, iteratorInfo.EnumerableKind );
            Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(object) ), iteratorInfo.ItemType );
        }
    }

    [Fact]
    public void GenericEnumerableNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

            Assert.False( iteratorInfo.IsIteratorMethod );
            Assert.Equal( EnumerableKind.IEnumerable, iteratorInfo.EnumerableKind );
            Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
        }
    }

    [Fact]
    public void GenericEnumeratorNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
    IEnumerator<int> Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.False( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IEnumerator, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void AsyncEnumerableNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
 IAsyncEnumerable<int> Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.False( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IAsyncEnumerable, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }

    [Fact]
    public void AsyncEnumeratorNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
 IAsyncEnumerator<int> Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

        Assert.False( iteratorInfo.IsIteratorMethod );
        Assert.Equal( EnumerableKind.IAsyncEnumerator, iteratorInfo.EnumerableKind );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), iteratorInfo.ItemType );
    }
#endif

    [Fact]
    public void NonGenericEnumeratorNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections;
class C
{
    IEnumerator Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

            Assert.False( iteratorInfo.IsIteratorMethod );
            Assert.Equal( EnumerableKind.UntypedIEnumerator, iteratorInfo.EnumerableKind );
            Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(object) ), iteratorInfo.ItemType );
        }
    }

    [Fact]
    public void NonGenericEnumerableNonYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections;
class C
{
    IEnumerable Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var iteratorInfo = compilation.Types.Single().Methods.Single().GetIteratorInfo();

            Assert.False( iteratorInfo.IsIteratorMethod );
            Assert.Equal( EnumerableKind.UntypedIEnumerable, iteratorInfo.EnumerableKind );
            Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(object) ), iteratorInfo.ItemType );
        }
    }
}