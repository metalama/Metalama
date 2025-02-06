// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class CacheKeyBuilderTests : BaseCachingTests
    {
        private sealed class MyCacheKeyBuilder : CacheKeyBuilder
        {
#pragma warning disable SA1401
            public string? LastMethodKey;
#pragma warning restore SA1401

            public override string BuildMethodKey(
                CachedMethodMetadata metadata,
                object? instance,
                IList<object?> arguments )
            {
                return this.LastMethodKey = base.BuildMethodKey( metadata, instance, arguments );
            }

            public MyCacheKeyBuilder( IFormatterRepository formatterRepository, CacheKeyBuilderOptions options ) : base( formatterRepository, options ) { }
        }

        private void DoTestMethod( string profileName, string expectedKey, Func<string> action )
        {
            using var context = this.InitializeTest( profileName, b => b.WithKeyBuilder( ( f, o ) => new MyCacheKeyBuilder( f, o ) ) );

            var keyBuilder = (MyCacheKeyBuilder) CachingService.Default.KeyBuilder;
            action();
            Console.WriteLine( keyBuilder.LastMethodKey );
            Assert.Equal( expectedKey, keyBuilder.LastMethodKey );
        }

        private async Task DoTestMethodAsync( string profileName, string expectedKey, Func<Task<string>> action )
        {
            await using var context = this.InitializeTest( profileName, b => b.WithKeyBuilder( ( f, o ) => new MyCacheKeyBuilder( f, o ) ) );

            var keyBuilder = (MyCacheKeyBuilder) CachingService.Default.KeyBuilder;
            await action();
            Console.WriteLine( keyBuilder.LastMethodKey );
            Assert.Equal( expectedKey, keyBuilder.LastMethodKey );
        }

#pragma warning disable CA1822 // Mark members as static

        private const string _profileNamePrefix = "Caching.Tests.CacheKeyBuilderTests_";

        #region TestInstanceMethod

        private const string _testInstanceMethodProfileName = _profileNamePrefix + "TestInstanceMethod";

        [Cache( ProfileName = _testInstanceMethodProfileName )]
        private string CachedInstanceMethod()
        {
            return "";
        }

        [Fact]
        public void TestInstanceMethod()
        {
            this.DoTestMethod(
                _testInstanceMethodProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethod(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests})",
                this.CachedInstanceMethod );
        }

        #endregion TestInstanceMethod

        #region TestInstanceMethodAsync

        private const string _testInstanceMethodAsyncProfileName = _profileNamePrefix + "TestInstanceMethodAsync";

        [Cache( ProfileName = _testInstanceMethodAsyncProfileName )]
        private async Task<string> CachedInstanceMethodAsync()
        {
            await Task.Yield();

            return "";
        }

        [Fact]
        public async Task TestInstanceMethodAsync()
        {
            await this.DoTestMethodAsync(
                _testInstanceMethodAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethodAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests})",
                this.CachedInstanceMethodAsync );
        }

        #endregion TestInstanceMethodAsync

        #region TestStaticMethod

        private const string _testStaticMethodProfileName = _profileNamePrefix + "TestStaticMethod";

        [Cache( ProfileName = _testStaticMethodProfileName )]
        private static string CachedStaticMethod()
        {
            return "";
        }

        [Fact]
        public void TestStaticMethod()
        {
            this.DoTestMethod(
                _testStaticMethodProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedStaticMethod()",
                CachedStaticMethod );
        }

        #endregion TestStaticMethod

        #region TestStaticMethodAsync

        private const string _testStaticMethodAsyncProfileName = _profileNamePrefix + "TestStaticMethodAsync";

        [Cache( ProfileName = _testStaticMethodAsyncProfileName )]
        private static async Task<string> CachedStaticMethodAsync()
        {
            await Task.Yield();

            return "";
        }

        [Fact]
        public async Task TestStaticMethodAsync()
        {
            await this.DoTestMethodAsync(
                _testStaticMethodAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedStaticMethodAsync()",
                CachedStaticMethodAsync );
        }

        #endregion TestStaticMethodAsync

        #region TestMethodWithParameters

        private const string _testMethodWithParametersProfileName = _profileNamePrefix + "TestMethodWithParameters";

        [Cache( ProfileName = _testMethodWithParametersProfileName )]
        private string CachedInstanceMethodWithParameters( int intParam, string? stringParam, object? objectParam )
        {
            return "CachedInstanceMethodWithParameters1";
        }

        [Cache( ProfileName = _testMethodWithParametersProfileName )]
        private string CachedInstanceMethodWithParameters( int intParam, object? objectParam1, object? objectParam2 )
        {
            return "CachedInstanceMethodWithParameters2";
        }

        [Fact]
        public void TestMethodWithParameters()
        {
            this.DoTestMethod(
                _testMethodWithParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethodWithParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (int) 0, (string) null, (object) null)",
                () => this.CachedInstanceMethodWithParameters( 0, null, null ) );
        }

        #endregion TestMethodWithParameters

        #region TestMethodWithParametersAsync

        private const string _testMethodWithParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithParametersAsync";

        [Cache( ProfileName = _testMethodWithParametersAsyncProfileName )]
        private async Task<string> CachedInstanceMethodWithParametersAsync( int intParam, string? stringParam, object? objectParam )
        {
            await Task.Yield();

            return "CachedInstanceMethodWithParametersAsync1";
        }

        [Cache( ProfileName = _testMethodWithParametersAsyncProfileName )]
        private async Task<string> CachedInstanceMethodWithParametersAsync( int intParam, object? objectParam1, object? objectParam2 )
        {
            await Task.Yield();

            return "CachedInstanceMethodWithParametersAsync2";
        }

        [Fact]
        public async Task TestMethodWithParametersAsync()
        {
            await this.DoTestMethodAsync(
                _testMethodWithParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethodWithParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (int) 0, (string) null, (object) null)",
                () => this.CachedInstanceMethodWithParametersAsync( 0, null, null ) );
        }

        #endregion TestMethodWithParametersAsync

        #region TestMethodWithIgnoredParameters

        private const string _testMethodWithIgnoredParametersProfileName = _profileNamePrefix + "TestMethodWithIgnoredParameters";

        [Cache( ProfileName = _testMethodWithIgnoredParametersProfileName )]
        private string CachedInstanceMethodWithIgnoredParameters( int intParam, object? objectParam1, [NotCacheKey] int ignored )
        {
            return "CachedInstanceMethodWithIgnoredParameters";
        }

        [Fact]
        public void TestMethodWithIgnoredParameters()
        {
            this.DoTestMethod(
                _testMethodWithIgnoredParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethodWithIgnoredParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (int) 0, (object) null, (int) *)",
                () => this.CachedInstanceMethodWithIgnoredParameters( 0, null, -1 ) );
        }

        #endregion TestMethodWithIgnoredParameters

        #region TestMethodWithIgnoredParametersAsync

        private const string _testMethodWithIgnoredParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithIgnoredParametersAsync";

        [Cache( ProfileName = _testMethodWithIgnoredParametersAsyncProfileName )]
        private async Task<string> CachedInstanceMethodWithIgnoredParametersAsync( int intParam, object? objectParam1, [NotCacheKey] int ignored )
        {
            await Task.Yield();

            return "CachedInstanceMethodWithIgnoredParametersAsync";
        }

        [Fact]
        public async Task TestMethodWithIgnoredParametersAsync()
        {
            await this.DoTestMethodAsync(
                _testMethodWithIgnoredParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.CachedInstanceMethodWithIgnoredParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (int) 0, (object) null, (int) *)",
                () => this.CachedInstanceMethodWithIgnoredParametersAsync( 0, null, -1 ) );
        }

        #endregion TestMethodWithIgnoredParametersAsync

        #region TestMethodWithIgnoredThisParameter

        private const string _testMethodWithIgnoredThisParameterProfileName = _profileNamePrefix + "TestMethodWithIgnoredThisParameter";

        [CachingConfiguration( ProfileName = _testMethodWithIgnoredThisParameterProfileName, IgnoreThisParameter = true )]
        private sealed class SomeClassWithIgnoredThisParameter
        {
            [Cache]
            public string SomeInstanceMethod()
            {
                return "SomeInstanceMethod";
            }
        }

        [Fact]
        public void TestMethodWithIgnoredThisParameter()
        {
            this.DoTestMethod(
                _testMethodWithIgnoredThisParameterProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.SomeClassWithIgnoredThisParameter.SomeInstanceMethod()",
                () => new SomeClassWithIgnoredThisParameter().SomeInstanceMethod() );
        }

        #endregion TestMethodWithIgnoredThisParameter

        #region TestMethodWithIgnoredThisParameterAsync

        private const string _testMethodWithIgnoredThisParameterAsyncProfileName = _profileNamePrefix + "TestMethodWithIgnoredThisParameterAsync";

        [CachingConfiguration( ProfileName = _testMethodWithIgnoredThisParameterAsyncProfileName, IgnoreThisParameter = true )]
        private sealed class SomeAsyncClassWithIgnoredThisParameter
        {
            [Cache]
            public async Task<string> SomeInstanceMethodAsync()
            {
                await Task.Yield();

                return "SomeInstanceMethod";
            }
        }

        [Fact]
        public async Task TestMethodWithIgnoredThisParameterAsync()
        {
            await this.DoTestMethodAsync(
                _testMethodWithIgnoredThisParameterAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.SomeAsyncClassWithIgnoredThisParameter.SomeInstanceMethodAsync()",
                () => new SomeAsyncClassWithIgnoredThisParameter().SomeInstanceMethodAsync() );
        }

        #endregion TestMethodWithIgnoredThisParameterAsync

        private class TestClassForCollections
        {
            public virtual string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return "CachedInstanceMethodWithCollectionParameters";
            }
        }

        private class AsyncTestClassForCollections
        {
            public virtual async Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                await Task.Yield();

                return "CachedInstanceMethodWithCollectionParameters";
            }
        }

        #region TestMethodWithNullCollectionParameters

        private const string _testMethodWithNullCollectionParametersProfileName = _profileNamePrefix + "TestMethodWithNullCollectionParameters";

        private sealed class TestClassForNullCollectionParameters : TestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithNullCollectionParametersProfileName )]
            public override string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParameters( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public void TestMethodWithNullCollectionParameters()
        {
            var testObject = new TestClassForNullCollectionParameters();

            this.DoTestMethod(
                _testMethodWithNullCollectionParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForNullCollectionParameters.CachedInstanceMethodWithCollectionParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForNullCollectionParameters}, (IEnumerable<int>) null, (IEnumerable<object>) null, (List<int>) null, (List<object>) null, (int[]) null, (object[]) null)",
                () => testObject.CachedInstanceMethodWithCollectionParameters(
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null! ) );
        }

        #endregion TestMethodWithNullCollectionParameters

        #region TestMethodWithNullCollectionParametersAsync

        private const string _testMethodWithNullCollectionParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithNullCollectionParametersAsync";

        private sealed class AsyncTestClassForNullCollectionParameters : AsyncTestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithNullCollectionParametersAsyncProfileName )]
            public override Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParametersAsync( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public async Task TestMethodWithNullCollectionParametersAsync()
        {
            var testObject = new AsyncTestClassForNullCollectionParameters();

            await this.DoTestMethodAsync(
                _testMethodWithNullCollectionParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForNullCollectionParameters.CachedInstanceMethodWithCollectionParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForNullCollectionParameters}, (IEnumerable<int>) null, (IEnumerable<object>) null, (List<int>) null, (List<object>) null, (int[]) null, (object[]) null)",
                () => testObject.CachedInstanceMethodWithCollectionParametersAsync(
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null! ) );
        }

        #endregion TestMethodWithNullCollectionParametersAsync

        #region TestMethodWithEmptyCollectionParameters

        private const string _testMethodWithEmptyCollectionParametersProfileName = _profileNamePrefix + "TestMethodWithEmptyCollectionParameters";

        private sealed class TestClassForEmptyCollectionParameters : TestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithEmptyCollectionParametersProfileName )]
            public override string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParameters( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public void TestMethodWithEmptyCollectionParameters()
        {
            var testObject = new TestClassForEmptyCollectionParameters();

            this.DoTestMethod(
                _testMethodWithEmptyCollectionParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForEmptyCollectionParameters.CachedInstanceMethodWithCollectionParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForEmptyCollectionParameters}, (IEnumerable<int>) [], (IEnumerable<object>) [], (List<int>) [], (List<object>) [], (int[]) [], (object[]) [])",
                () => testObject.CachedInstanceMethodWithCollectionParameters(
                    new List<int>(),
                    new List<object>(),
                    [],
                    [],
                    [],
                    [] ) );
        }

        #endregion TestMethodWithEmptyCollectionParameters

        #region TestMethodWithEmptyCollectionParametersAsync

        private const string _testMethodWithEmptyCollectionParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithEmptyCollectionParametersAsync";

        private sealed class AsyncTestClassForEmptyCollectionParameters : AsyncTestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithEmptyCollectionParametersAsyncProfileName )]
            public override Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParametersAsync( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public async Task TestMethodWithEmptyCollectionParametersAsync()
        {
            var testObject = new AsyncTestClassForEmptyCollectionParameters();

            await this.DoTestMethodAsync(
                _testMethodWithEmptyCollectionParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForEmptyCollectionParameters.CachedInstanceMethodWithCollectionParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForEmptyCollectionParameters}, (IEnumerable<int>) [], (IEnumerable<object>) [], (List<int>) [], (List<object>) [], (int[]) [], (object[]) [])",
                () => testObject.CachedInstanceMethodWithCollectionParametersAsync(
                    new List<int>(),
                    new List<object>(),
                    [],
                    [],
                    [],
                    [] ) );
        }

        #endregion TestMethodWithEmptyCollectionParametersAsync

        #region TestMethodWithOneItemCollectionParameters

        private const string _testMethodWithOneItemCollectionParametersProfileName = _profileNamePrefix + "TestMethodWithOneItemCollectionParameters";

        private sealed class TestClassForOneCollectionParameters : TestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithOneItemCollectionParametersProfileName )]
            public override string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParameters( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public void TestMethodWithOneItemCollectionParameters()
        {
            var testObject = new TestClassForOneCollectionParameters();

            this.DoTestMethod(
                _testMethodWithOneItemCollectionParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForOneCollectionParameters.CachedInstanceMethodWithCollectionParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForOneCollectionParameters}, (IEnumerable<int>) [ 1 ], (IEnumerable<object>) [ \"Object1\" ], (List<int>) [ 2 ], (List<object>) [ \"Object2\" ], (int[]) [ 3 ], (object[]) [ \"Object3\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParameters(
                    new List<int>( new[] { 1 } ),
                    new List<object>( new object[] { "Object1" } ),
                    [..new[] { 2 }],
                    [..new object[] { "Object2" }],
                    [3],
                    ["Object3"] ) );
        }

        #endregion TestMethodWithOneItemCollectionParameters

        #region TestMethodWithOneItemCollectionParametersAsync

        private const string _testMethodWithOneItemCollectionParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithOneItemCollectionParametersAsync";

        private sealed class AsyncTestClassForOneCollectionParameters : AsyncTestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithOneItemCollectionParametersAsyncProfileName )]
            public override Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParametersAsync( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public async Task TestMethodWithOneItemCollectionParametersAsync()
        {
            var testObject = new AsyncTestClassForOneCollectionParameters();

            await this.DoTestMethodAsync(
                _testMethodWithOneItemCollectionParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForOneCollectionParameters.CachedInstanceMethodWithCollectionParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForOneCollectionParameters}, (IEnumerable<int>) [ 1 ], (IEnumerable<object>) [ \"Object1\" ], (List<int>) [ 2 ], (List<object>) [ \"Object2\" ], (int[]) [ 3 ], (object[]) [ \"Object3\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParametersAsync(
                    new List<int>( new[] { 1 } ),
                    new List<object>( new object[] { "Object1" } ),
                    [..new[] { 2 }],
                    [..new object[] { "Object2" }],
                    [3],
                    ["Object3"] ) );
        }

        #endregion TestMethodWithOneItemCollectionParametersAsync

        #region TestMethodWithTwoItemCollectionParameters

        private const string _testMethodWithTwoItemCollectionParametersProfileName = _profileNamePrefix + "TestMethodWithTwoItemCollectionParameters";

        private sealed class TestClassForTwoCollectionParameters : TestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithTwoItemCollectionParametersProfileName )]
            public override string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParameters( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public void TestMethodWithTwoItemCollectionParameters()
        {
            var testObject = new TestClassForTwoCollectionParameters();

            this.DoTestMethod(
                _testMethodWithTwoItemCollectionParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForTwoCollectionParameters.CachedInstanceMethodWithCollectionParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForTwoCollectionParameters}, (IEnumerable<int>) [ 1, 2 ], (IEnumerable<object>) [ \"Object1\", \"Object2\" ], (List<int>) [ 3, 4 ], (List<object>) [ \"Object3\", \"Object4\" ], (int[]) [ 5, 6 ], (object[]) [ \"Object5\", \"Object6\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParameters(
                    new List<int>( new[] { 1, 2 } ),
                    new List<object>( new object[] { "Object1", "Object2" } ),
                    [..new[] { 3, 4 }],
                    [..new object[] { "Object3", "Object4" }],
                    [5, 6],
                    ["Object5", "Object6"] ) );
        }

        #endregion TestMethodWithTwoItemCollectionParameters

        #region TestMethodWithTwoItemCollectionParametersAsync

        private const string _testMethodWithTwoItemCollectionParametersAsyncProfileName = _profileNamePrefix + "TestMethodWithTwoItemCollectionParametersAsync";

        private sealed class AsyncTestClassForTwoCollectionParameters : AsyncTestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithTwoItemCollectionParametersAsyncProfileName )]
            public override Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParametersAsync( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public async Task TestMethodWithTwoItemCollectionParametersAsync()
        {
            var testObject = new AsyncTestClassForTwoCollectionParameters();

            await this.DoTestMethodAsync(
                _testMethodWithTwoItemCollectionParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForTwoCollectionParameters.CachedInstanceMethodWithCollectionParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForTwoCollectionParameters}, (IEnumerable<int>) [ 1, 2 ], (IEnumerable<object>) [ \"Object1\", \"Object2\" ], (List<int>) [ 3, 4 ], (List<object>) [ \"Object3\", \"Object4\" ], (int[]) [ 5, 6 ], (object[]) [ \"Object5\", \"Object6\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParametersAsync(
                    new List<int>( new[] { 1, 2 } ),
                    new List<object>( new object[] { "Object1", "Object2" } ),
                    [..new[] { 3, 4 }],
                    [..new object[] { "Object3", "Object4" }],
                    [5, 6],
                    ["Object5", "Object6"] ) );
        }

        #endregion TestMethodWithTwoItemCollectionParametersAsync

        #region TestMethodWithThreeItemCollectionParameters

        private const string _testMethodWithThreeItemCollectionParametersProfileName = _profileNamePrefix + "TestMethodWithThreeItemCollectionParameters";

        private sealed class TestClassForThreeCollectionParameters : TestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithThreeItemCollectionParametersProfileName )]
            public override string CachedInstanceMethodWithCollectionParameters(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParameters( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public void TestMethodWithThreeItemCollectionParameters()
        {
            var testObject = new TestClassForThreeCollectionParameters();

            this.DoTestMethod(
                _testMethodWithThreeItemCollectionParametersProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForThreeCollectionParameters.CachedInstanceMethodWithCollectionParameters(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.TestClassForThreeCollectionParameters}, (IEnumerable<int>) [ 1, 2, 3 ], (IEnumerable<object>) [ \"Object1\", \"Object2\", \"Object3\" ], (List<int>) [ 4, 5, 6 ], (List<object>) [ \"Object4\", \"Object5\", \"Object6\" ], (int[]) [ 7, 8, 9 ], (object[]) [ \"Object7\", \"Object8\", \"Object9\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParameters(
                    new List<int>( new[] { 1, 2, 3 } ),
                    new List<object>( new object[] { "Object1", "Object2", "Object3" } ),
                    [..new[] { 4, 5, 6 }],
                    [..new object[] { "Object4", "Object5", "Object6" }],
                    [7, 8, 9],
                    ["Object7", "Object8", "Object9"] ) );
        }

        #endregion TestMethodWithThreeItemCollectionParameters

        #region TestMethodWithThreeItemCollectionParametersAsync

        private const string _testMethodWithThreeItemCollectionParametersAsyncProfileName =
            _profileNamePrefix + "TestMethodWithThreeItemCollectionParametersAsync";

        private sealed class AsyncTestClassForThreeCollectionParameters : AsyncTestClassForCollections
        {
            [Cache( ProfileName = _testMethodWithThreeItemCollectionParametersAsyncProfileName )]
            public override Task<string> CachedInstanceMethodWithCollectionParametersAsync(
                IEnumerable<int> intEnumerable,
                IEnumerable<object> objectEnumerable,
                List<int> intList,
                List<object> objectList,
                int[] intArray,
                object[] objectArray )
            {
                return base.CachedInstanceMethodWithCollectionParametersAsync( intEnumerable, objectEnumerable, intList, objectList, intArray, objectArray );
            }
        }

        [Fact]
        public async Task TestMethodWithThreeItemCollectionParametersAsync()
        {
            var testObject = new AsyncTestClassForThreeCollectionParameters();

            await this.DoTestMethodAsync(
                _testMethodWithThreeItemCollectionParametersAsyncProfileName,
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForThreeCollectionParameters.CachedInstanceMethodWithCollectionParametersAsync(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.AsyncTestClassForThreeCollectionParameters}, (IEnumerable<int>) [ 1, 2, 3 ], (IEnumerable<object>) [ \"Object1\", \"Object2\", \"Object3\" ], (List<int>) [ 4, 5, 6 ], (List<object>) [ \"Object4\", \"Object5\", \"Object6\" ], (int[]) [ 7, 8, 9 ], (object[]) [ \"Object7\", \"Object8\", \"Object9\" ])",
                () => testObject.CachedInstanceMethodWithCollectionParametersAsync(
                    new List<int>( new[] { 1, 2, 3 } ),
                    new List<object>( new object[] { "Object1", "Object2", "Object3" } ),
                    [..new[] { 4, 5, 6 }],
                    [..new object[] { "Object4", "Object5", "Object6" }],
                    [7, 8, 9],
                    ["Object7", "Object8", "Object9"] ) );
        }

        #endregion TestMethodWithThreeItemCollectionParametersAsync

        public CacheKeyBuilderTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}