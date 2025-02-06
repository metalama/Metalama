// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Implementations;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Flashtrace.Formatters.UnitTests;

public class TypeFormatterTests : FormattersTestsBase
{
    private TypeFormatter DefaultTypeFormatter { get; }

    public TypeFormatterTests( ITestOutputHelper logger ) : base( logger )
    {
        this.DefaultTypeFormatter = new TypeFormatter( CreateRepository() );
    }

    private void TestFormatter( Type type, string expectedString )
    {
        var sb = new UnsafeStringBuilder();
        this.DefaultTypeFormatter.Format( sb, type );
        Assert.Equal( expectedString, sb.ToString() );
    }

    [Fact]
    public void TestPrimitives()
    {
        this.TestFormatter( typeof(string), "string" );
        this.TestFormatter( typeof(decimal), "decimal" );
    }

    [Fact]
    public void TestArray()
    {
        this.TestFormatter( typeof(DateTime[]), "DateTime[]" );
        this.TestFormatter( typeof(string[]), "string[]" );
        this.TestFormatter( typeof(string[,]), "string[,]" );
        this.TestFormatter( typeof(string[,,]), "string[,,]" );
        this.TestFormatter( typeof(int?[]), "int?[]" );
    }

    [Fact]
    public void TestNullable()
    {
        this.TestFormatter( typeof(int?), "int?" );
        this.TestFormatter( typeof(DateTime?), "DateTime?" );
    }

    [Fact]
    public void TestSystemNamespace()
    {
        this.TestFormatter( typeof(DateTime), "DateTime" );
        this.TestFormatter( typeof(IEnumerable), "IEnumerable" );
        this.TestFormatter( typeof(Stream), "System.IO.Stream" );
    }

    [Fact]
    public void TestGenerics()
    {
        this.TestFormatter( typeof(IEquatable<>), "IEquatable<>" );
        this.TestFormatter( typeof(IEquatable<DateTime>), "IEquatable<DateTime>" );
        this.TestFormatter( typeof(IDictionary<,>), "IDictionary<,>" );
        this.TestFormatter( typeof(IEquatable<Tuple<int>>), "IEquatable<Tuple<int>>" );
        this.TestFormatter( typeof(Tuple<,>), "Tuple<,>" );
    }

    [Fact]
    public void TestInnerTypes()
    {
        this.TestFormatter( typeof(TestType.InnerType), "Flashtrace.Formatters.UnitTests.TestType.InnerType" );
        this.TestFormatter( typeof(IEnumerable<TestType.InnerType>), "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType>" );
        this.TestFormatter( typeof(TestType.InnerType.MoreInnerType), "Flashtrace.Formatters.UnitTests.TestType.InnerType.MoreInnerType" );

        this.TestFormatter(
            typeof(IEnumerable<TestType.InnerType.MoreInnerType>),
            "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType.MoreInnerType>" );

        this.TestFormatter(
            typeof(TestType.InnerType.MoreInnerType.EvenMoreInnerType),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType.MoreInnerType.EvenMoreInnerType" );

        this.TestFormatter(
            typeof(IEnumerable<TestType.InnerType.MoreInnerType.EvenMoreInnerType>),
            "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType.MoreInnerType.EvenMoreInnerType>" );
    }

    [Fact]
    public void TestGenericInnerTypes()
    {
        this.TestFormatter(
            typeof(TestType.InnerType<,>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<,>" );

        this.TestFormatter(
            typeof(TestType.InnerType<,>.MoreInnerType<,>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<,>.MoreInnerType<,>" );

        this.TestFormatter(
            typeof(TestType.InnerType<,>.MoreInnerType<,>.EvenMoreInnerType<,>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<,>.MoreInnerType<,>.EvenMoreInnerType<,>" );

        this.TestFormatter(
            typeof(TestType.InnerType<TestType1, TestType2>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>" );

        this.TestFormatter(
            typeof(IEnumerable<TestType.InnerType<TestType1, TestType2>>),
            "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>>" );

        this.TestFormatter(
            typeof(TestType.InnerType<TestType1, TestType2>.MoreInnerType<TestType3, TestType4>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>.MoreInnerType<Flashtrace.Formatters.UnitTests.TestType3,Flashtrace.Formatters.UnitTests.TestType4>" );

        this.TestFormatter(
            typeof(IEnumerable<TestType.InnerType<TestType1, TestType2>.MoreInnerType<TestType3, TestType4>>),
            "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>.MoreInnerType<Flashtrace.Formatters.UnitTests.TestType3,Flashtrace.Formatters.UnitTests.TestType4>>" );

        this.TestFormatter(
            typeof(TestType.InnerType<TestType1, TestType2>.MoreInnerType<TestType3, TestType4>.EvenMoreInnerType<TestType5, TestType6>),
            "Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>.MoreInnerType<Flashtrace.Formatters.UnitTests.TestType3,Flashtrace.Formatters.UnitTests.TestType4>.EvenMoreInnerType<Flashtrace.Formatters.UnitTests.TestType5,Flashtrace.Formatters.UnitTests.TestType6>" );

        this.TestFormatter(
            typeof(IEnumerable<TestType.InnerType<TestType1, TestType2>.MoreInnerType<TestType3, TestType4>.EvenMoreInnerType<TestType5, TestType6>>),
            "IEnumerable<Flashtrace.Formatters.UnitTests.TestType.InnerType<Flashtrace.Formatters.UnitTests.TestType1,Flashtrace.Formatters.UnitTests.TestType2>.MoreInnerType<Flashtrace.Formatters.UnitTests.TestType3,Flashtrace.Formatters.UnitTests.TestType4>.EvenMoreInnerType<Flashtrace.Formatters.UnitTests.TestType5,Flashtrace.Formatters.UnitTests.TestType6>>" );
    }
}

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedTypeParameter
// ReSharper disable RedundantTypeDeclarationBody
#pragma warning disable SA1314
#pragma warning disable SA1402

public class TestType
{
    public class InnerType
    {
        public class MoreInnerType
        {
            public class EvenMoreInnerType { }
        }
    }

    public class InnerType<T1, T2>
    {
        public class MoreInnerType<U1, U2>
        {
            public class EvenMoreInnerType<V1, V2> { }
        }
    }
}

public class TestType1 { }

public class TestType2 { }

public class TestType3 { }

public class TestType4 { }

public class TestType5 { }

public class TestType6 { }