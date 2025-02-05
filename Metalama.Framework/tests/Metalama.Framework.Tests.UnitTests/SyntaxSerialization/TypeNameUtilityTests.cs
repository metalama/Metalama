// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets;
using System;
using System.Collections.Generic;
using Xunit;

// ReSharper disable BadListLineBreaks
// ReSharper disable MemberCanBeInternal

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    // ReSharper disable UnusedTypeParameter
    // ReSharper disable ClassNeverInstantiated.Global

    public sealed class TypeNameUtilityTests : SerializerTestsBase
    {
        [Fact]
        public void TestTypeNameUtility()
        {
            var typesToTest = new Dictionary<string, Type>
            {
                { "global::System.String", typeof(string) },
                { "global::System.String[]", typeof(string[]) },
                { "global::System.Object[]", typeof(object[]) },
                { "global::System.Boolean[]", typeof(bool[]) },
                { "global::System.Object", typeof(object) },
                { "global::System.Int32", typeof(int) },
                { "global::System.Double", typeof(double) },
                { "global::System.Single", typeof(float) },
                { "global::System.Boolean", typeof(bool) },
                { "global::System.Char", typeof(char) },
                { "global::System.Decimal", typeof(decimal) },
                { "global::System.Decimal?[]", typeof(decimal?[]) },
                { "global::System.Decimal?[][]", typeof(decimal?[][]) },
                { "global::System.Int64", typeof(long) },
                { "global::System.Guid", typeof(Guid) },
                { "global::System.Int32?", typeof(int?) },
                { "global::System.Double?", typeof(double?) },
                { "global::System.Single?", typeof(float?) },
                { "global::System.Boolean?", typeof(bool?) },
                { "global::System.Char?", typeof(char?) },
                { "global::System.Decimal?", typeof(decimal?) },
                { "global::System.Int64?", typeof(long?) },
                { "global::System.Guid?", typeof(Guid?) },
                { "global::System.Collections.Generic.List<global::System.String>", typeof(List<string>) },
                { "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid>", typeof(Dictionary<string, Guid>) },
                { "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid>[]", typeof(Dictionary<string, Guid>[]) },
                { "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid?>", typeof(Dictionary<string, Guid?>) },
                {
                    "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid?>>",
                    typeof(Dictionary<string, Dictionary<string, Guid?>>)
                },
                {
                    "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid?>>[]",
                    typeof(Dictionary<string, Dictionary<string, Guid?>>[])
                },
                {
                    "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Guid?>>[][]",
                    typeof(Dictionary<string, Dictionary<string, Guid?>>[][])
                },
                { "global::System.Int32[]", typeof(int[]) },
                { "global::System.Int32[][]", typeof(int[][]) },
                { "global::System.Int32[][][]", typeof(int[][][]) },
                { "global::System.Int32[][][][]", typeof(int[][][][]) },
                { "global::System.Int32[][][][][]", typeof(int[][][][][]) },
                { "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass", typeof(TestClass) },
                {
                    "global::System.Collections.Generic.List<global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass>",
                    typeof(List<TestClass>)
                },
                {
                    "global::System.Collections.Generic.Dictionary<global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass,global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass>",
                    typeof(Dictionary<TestClass, TestClass>)
                },
                {
                    "global::System.Collections.Generic.Dictionary<global::System.String,global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass>",
                    typeof(Dictionary<string, TestClass>)
                },
                {
                    "global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<global::System.String,global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.TestClass>>",
                    typeof(List<Dictionary<string, TestClass>>)
                },
                {
                    "global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<global::System.String,global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String>>>",
                    typeof(List<Dictionary<string, GenericTestClass<string>>>)
                },
                {
                    "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String>.SecondSubType<global::System.Decimal>",
                    typeof(GenericTestClass<string>.SecondSubType<decimal>)
                },
                {
                    "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String>.SecondSubType",
                    typeof(GenericTestClass<string>.SecondSubType)
                },
                {
                    "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String,global::System.Int32>.SecondSubType",
                    typeof(GenericTestClass<string, int>.SecondSubType)
                },
                {
                    "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Int32>>.SecondSubType<global::System.String>",
                    typeof(GenericTestClass<string, Dictionary<string, int>>.SecondSubType<string>)
                },
                {
                    "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Int32>>.SecondSubType<global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.GenericTestClass<global::System.String,global::System.Collections.Generic.Dictionary<global::System.String,global::System.Int32>>>",
                    typeof(GenericTestClass<string, Dictionary<string, int>>.SecondSubType<GenericTestClass<string, Dictionary<string, int>>>)
                }
            };

            foreach ( var t in typesToTest )
            {
                using var testContext = this.CreateSerializationTestContext( "" );

                var actualName = testContext.SerializationContext.GetTypeSyntax( t.Value ).ToString();
                Assert.Equal( t.Key, actualName );
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single type

    public class GenericTestClass<T1, T2>
    {
        public sealed class SecondSubType;

        public sealed class SecondSubType<T3>;
    }
}