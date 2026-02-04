// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        #region Hashing Tests

        private sealed class HashingCacheKeyBuilder : CacheKeyBuilder
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

            public HashingCacheKeyBuilder( IFormatterRepository formatterRepository, CacheKeyBuilderOptions options )
                : base( formatterRepository, options ) { }
        }

        private string DoTestMethodWithHashingAndCapture(
            string profileName,
            CacheKeyHashingAlgorithm algorithm,
            int threshold,
            Func<string> action )
        {
            using var context = this.InitializeTest(
                profileName,
                b => b.WithKeyBuilder(
                    ( f, _ ) => new HashingCacheKeyBuilder(
                        f,
                        new CacheKeyBuilderOptions { HashingAlgorithm = algorithm, KeyCompressingThreshold = threshold } ) ) );

            var keyBuilder = (HashingCacheKeyBuilder) CachingService.Default.KeyBuilder;
            action();
            this.TestOutputHelper.WriteLine( $"Key: {keyBuilder.LastMethodKey}" );

            return keyBuilder.LastMethodKey!;
        }

        #region No Hashing - Method Keys

        private const string _noHashingProfileName = _profileNamePrefix + "NoHashing";

        [Cache( ProfileName = _noHashingProfileName )]
        private string MethodWithNoHashing( string param ) => param;

        [Fact]
        public void TestMethodKey_NoHashing_InstanceMethod()
        {
            var key = this.DoTestMethodWithHashingAndCapture(
                _noHashingProfileName,
                CacheKeyHashingAlgorithm.None,
                1000,
                () => this.MethodWithNoHashing( "value1" ) );

            Assert.Equal(
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodWithNoHashing(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (string) \"value1\")",
                key );
        }

        #endregion

        #region XxHash64 - Method Keys

        private const string _xxHash64AboveThresholdProfile = _profileNamePrefix + "XxHash64_AboveThreshold";

        [Cache( ProfileName = _xxHash64AboveThresholdProfile )]
        private string MethodForAboveThreshold( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash64_AboveThreshold()
        {
            var key = this.DoTestMethodWithHashingAndCapture(
                _xxHash64AboveThresholdProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForAboveThreshold( "this is a long parameter value" ) );

            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForAboveThreshold~DsMAFM6LcmI", key );
        }

        private const string _xxHash64BelowThresholdProfile = _profileNamePrefix + "XxHash64_BelowThreshold";

        [Cache( ProfileName = _xxHash64BelowThresholdProfile )]
        private string MethodForBelowThreshold( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash64_BelowThreshold()
        {
            var key = this.DoTestMethodWithHashingAndCapture(
                _xxHash64BelowThresholdProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                1000,
                () => this.MethodForBelowThreshold( "short" ) );

            Assert.Equal(
                "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForBelowThreshold(this={Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests}, (string) \"short\")",
                key );
        }

        private const string _xxHash64ConsistentProfile = _profileNamePrefix + "XxHash64_Consistent";

        [Cache( ProfileName = _xxHash64ConsistentProfile )]
        private string MethodForConsistent( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash64_ConsistentHash()
        {
            var key1 = this.DoTestMethodWithHashingAndCapture(
                _xxHash64ConsistentProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForConsistent( "consistent value for hashing" ) );

            var key2 = this.DoTestMethodWithHashingAndCapture(
                _xxHash64ConsistentProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForConsistent( "consistent value for hashing" ) );

            Assert.Equal( key1, key2 );
            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForConsistent~oCvsjcu1zvU", key1 );
        }

        private const string _xxHash64DifferentProfile = _profileNamePrefix + "XxHash64_Different";

        [Cache( ProfileName = _xxHash64DifferentProfile )]
        private string MethodForDifferent( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash64_DifferentInputs()
        {
            var key1 = this.DoTestMethodWithHashingAndCapture(
                _xxHash64DifferentProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForDifferent( "first long parameter value here" ) );

            var key2 = this.DoTestMethodWithHashingAndCapture(
                _xxHash64DifferentProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForDifferent( "second long parameter value here" ) );

            Assert.NotEqual( key1, key2 );
            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForDifferent~AGCxesDBay0", key1 );
            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForDifferent~0NrrsjHRYIs", key2 );
        }

        #endregion

        #region XxHash128 - Method Keys

        private const string _xxHash128AboveThresholdProfile = _profileNamePrefix + "XxHash128_AboveThreshold";

        [Cache( ProfileName = _xxHash128AboveThresholdProfile )]
        private string MethodForXxHash128Above( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash128_AboveThreshold()
        {
            var key = this.DoTestMethodWithHashingAndCapture(
                _xxHash128AboveThresholdProfile,
                CacheKeyHashingAlgorithm.XxHash128,
                50,
                () => this.MethodForXxHash128Above( "this is a long parameter value" ) );

            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForXxHash128Above~scmoaRBwPbyLBp/q/WBW/A", key );
        }

        private const string _xxHash64LongerProfile = _profileNamePrefix + "XxHash64_Longer";
        private const string _xxHash128LongerProfile = _profileNamePrefix + "XxHash128_Longer";

        [Cache( ProfileName = _xxHash64LongerProfile )]
        private string MethodForLonger64( string param ) => param;

        [Cache( ProfileName = _xxHash128LongerProfile )]
        private string MethodForLonger128( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash128_LongerHashThanXxHash64()
        {
            var key64 = this.DoTestMethodWithHashingAndCapture(
                _xxHash64LongerProfile,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.MethodForLonger64( "same long parameter value here" ) );

            var key128 = this.DoTestMethodWithHashingAndCapture(
                _xxHash128LongerProfile,
                CacheKeyHashingAlgorithm.XxHash128,
                50,
                () => this.MethodForLonger128( "same long parameter value here" ) );

            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForLonger64~AOB+nmPuWks", key64 );
            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.MethodForLonger128~hW/a3ACvH9lzz3ykAcFYiA", key128 );
            Assert.True( key128.Length > key64.Length );
        }

        #endregion

        #region Static Method - Hashing

        private const string _staticHashingProfileName = _profileNamePrefix + "StaticHashing";

        [Cache( ProfileName = _staticHashingProfileName )]
        private static string StaticMethodWithHashing( string param ) => param;

        [Fact]
        public void TestMethodKey_XxHash64_StaticMethod()
        {
            using var context = this.InitializeTest(
                _staticHashingProfileName,
                b => b.WithKeyBuilder(
                    ( f, _ ) => new HashingCacheKeyBuilder(
                        f,
                        new CacheKeyBuilderOptions { HashingAlgorithm = CacheKeyHashingAlgorithm.XxHash64, KeyCompressingThreshold = 50 } ) ) );

            var keyBuilder = (HashingCacheKeyBuilder) CachingService.Default.KeyBuilder;
            StaticMethodWithHashing( "long static method parameter value" );
            var key = keyBuilder.LastMethodKey!;
            this.TestOutputHelper.WriteLine( $"Key: {key}" );

            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.StaticMethodWithHashing~ao+90DFkieM", key );
            Assert.DoesNotContain( "this=", key, StringComparison.Ordinal );
        }

        #endregion

        #region Generic Method - Hashing

        private const string _genericHashingProfileName = _profileNamePrefix + "GenericHashing";

        [Cache( ProfileName = _genericHashingProfileName )]
        private T GenericMethodWithHashing<T>( T value ) where T : class => value;

        [Fact]
        public void TestMethodKey_XxHash64_GenericMethod()
        {
            var key = this.DoTestMethodWithHashingAndCapture(
                _genericHashingProfileName,
                CacheKeyHashingAlgorithm.XxHash64,
                50,
                () => this.GenericMethodWithHashing( "long generic method parameter" ) );

            // Generic arguments should be stripped from the prefix
            Assert.Equal( "Metalama.Patterns.Caching.Tests.CacheKeyBuilderTests.GenericMethodWithHashing~NLzohCwEu3U", key );
            Assert.DoesNotContain( "<string>", key, StringComparison.Ordinal );
        }

        #endregion

        #region Dependency Keys - Hashing

        [Fact]
        public void TestDependencyKey_NoHashing()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance, _ => { } );

            using var builder = new CacheKeyBuilder(
                formatters,
                new CacheKeyBuilderOptions { HashingAlgorithm = CacheKeyHashingAlgorithm.None } );

            var key = builder.BuildDependencyKey( "test dependency" );
            this.TestOutputHelper.WriteLine( $"Key: {key}" );

            Assert.Equal( "\"test dependency\"", key );
        }

        [Fact]
        public void TestDependencyKey_XxHash64_AboveThreshold()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance, _ => { } );

            using var builder = new CacheKeyBuilder(
                formatters,
                new CacheKeyBuilderOptions { HashingAlgorithm = CacheKeyHashingAlgorithm.XxHash64, KeyCompressingThreshold = 10 } );

            var key = builder.BuildDependencyKey( "long dependency key value" );
            this.TestOutputHelper.WriteLine( $"Key: {key}" );

            Assert.Equal( "+xzXcfxonoE", key );
            Assert.DoesNotContain( "~", key, StringComparison.Ordinal );
        }

        [Fact]
        public void TestDependencyKey_XxHash128_AboveThreshold()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance, _ => { } );

            using var builder = new CacheKeyBuilder(
                formatters,
                new CacheKeyBuilderOptions { HashingAlgorithm = CacheKeyHashingAlgorithm.XxHash128, KeyCompressingThreshold = 10 } );

            var key = builder.BuildDependencyKey( "long dependency key value" );
            this.TestOutputHelper.WriteLine( $"Key: {key}" );

            Assert.Equal( "XvAena8w0848u3Se8hp2mg", key );
            Assert.DoesNotContain( "~", key, StringComparison.Ordinal );
        }

        [Fact]
        public void TestDependencyKey_XxHash64_BelowThreshold()
        {
            var formatters = FormatterRepository.Create( CacheKeyFormatting.Instance, _ => { } );

            using var builder = new CacheKeyBuilder(
                formatters,
                new CacheKeyBuilderOptions { HashingAlgorithm = CacheKeyHashingAlgorithm.XxHash64, KeyCompressingThreshold = 1000 } );

            var key = builder.BuildDependencyKey( "short" );
            this.TestOutputHelper.WriteLine( $"Key: {key}" );

            Assert.Equal( "\"short\"", key );
        }

        #endregion

        #endregion Hashing Tests
    }
}