// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Flashtrace.Formatters.UnitTests;

public class FormattersTests : FormattersTestsBase
{
    public FormattersTests( ITestOutputHelper logger )
        : base( logger ) { }

    private struct TestStruct
    {
        public override string ToString()
        {
            return "ToString";
        }
    }

    private sealed class TestStructFormatter : Formatter<TestStruct>
    {
        public TestStructFormatter( IFormatterRepository repository ) : base( repository ) { }

        public override void Format( UnsafeStringBuilder stringBuilder, TestStruct value )
        {
            stringBuilder.Append( "formatter" );
        }
    }

    [Fact]
    public void StronglyTypedUnregisteredType()
    {
        _ = CreateRepository().Get<TestStruct>();
    }

    [Fact]
    public void NullableUsesFormatter()
    {
        var repo = CreateRepository( b => b.AddFormatter( x => new TestStructFormatter( x ) ) );

        Assert.Equal( "formatter", this.Format<TestStruct?>( repo, default(TestStruct) ) );
    }

    [Fact]
    public void NullableNull()
    {
        var repo = CreateRepository( b => b.AddFormatter( x => new TestStructFormatter( x ) ) );

        Assert.Equal( "null", this.Format<TestStruct?>( repo, null ) );
    }

    [Fact]
    public void Formattable()
    {
        Assert.Equal( "Formattable", this.FormatDefault( new FormattableObject() ) );
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void SpanFormattable()
    {
        Assert.Equal( "SpanFormattable", this.FormatDefault( new SpanFormattableObject() ) );
    }
#endif

    [Fact]
    public void NullToString()
    {
        Assert.Equal( "{null}", this.FormatDefault( new NullToStringClass() ) );
    }

    [Fact]
    public void StronglyTypedAnonymous()
    {
        var anonymous = new { A = "a", B = 0 };
        Assert.True( anonymous.GetType().IsAnonymous() );

        Assert.Equal( "{ A = \"a\", B = 0 }", this.FormatDefault( anonymous ) );
    }

    [Fact]
    public void WeaklyTypedAnonymous()
    {
        object anonymous = new { A = "a", B = 0 };
        Assert.True( anonymous.GetType().IsAnonymous() );

        Assert.Equal( "{ A = \"a\", B = 0 }", this.FormatDefault( anonymous ) );
    }

    [Fact]
    public void Types()
    {
        Assert.Equal( "null", this.FormatDefault<Type>( null ) );
        Assert.Equal( "int[]", this.FormatDefault( typeof(int[]) ) );
        Assert.Equal( "List<int>", this.FormatDefault( typeof(List<int>) ) );
    }

    [Fact]
    public void MethodInfo()
    {
        Assert.Equal( "null", this.FormatDefault<MethodInfo>( null ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.Method1()",
            this.FormatDefault(
                typeof(SomeType).GetMethod( nameof(SomeType.Method1), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.Method2(int)",
            this.FormatDefault(
                typeof(SomeType).GetMethod( nameof(SomeType.Method2), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.Method3(int&)",
            this.FormatDefault(
                typeof(SomeType).GetMethod( nameof(SomeType.Method3), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.Method4<T>(List<T>)",
            this.FormatDefault(
                typeof(SomeType).GetMethod( nameof(SomeType.Method4), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) );
    }

    [Fact]
    public void ConstructorInfo()
    {
        Assert.Equal( "null", this.FormatDefault<ConstructorInfo>( null ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.new()",
            this.FormatDefault(
                typeof(SomeType).GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                    .Single( c => c.GetParameters().Length == 0 ) ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.new(int)",
            this.FormatDefault(
                typeof(SomeType).GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                    .Single( c => c.GetParameters().Length == 1 ) ) );

        Assert.Equal(
            "Flashtrace.Formatters.UnitTests.FormattersTests.SomeType.StaticConstructor()",
            this.FormatDefault(
                typeof(SomeType).GetConstructors( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
                    .Single( c => c.GetParameters().Length == 0 ) ) );
    }

    // ReSharper disable UnusedMember.Local
    // ReSharper disable once NotAccessedField.Local

    private sealed class SomeType
    {
#pragma warning disable CS0414
#pragma warning disable IDE0044
#pragma warning disable IDE0052
#pragma warning disable IDE1006
        private static int staticField;
        private int instanceField;
#pragma warning restore IDE1006
#pragma warning restore IDE0052
#pragma warning restore IDE0044
#pragma warning restore CS0414

        static SomeType()
        {
            staticField = 1;
        }

        public SomeType()
        {
            this.instanceField = 1;
        }

        public SomeType( int a )
        {
            this.instanceField = a;
        }

#pragma warning disable CA1822
        public void Method1() { }

        public int Method2( int a ) { return a; }

        public void Method3( out int a ) { a = 0; }

        public void Method4<T>( List<T> l ) { }
#pragma warning restore CA1822
    }

    [Fact]
    public void DifferentRepositoriesAreDifferent()
    {
        var repo1 = CreateRepository( b => b.AddFormatter( r => new TestStructFormatter( r ) ) );

        var repo2 = CreateRepository();

        Assert.Equal( "formatter", this.Format( repo1, default(TestStruct) ) );
        Assert.Equal( "{ToString}", this.Format( repo2, default(TestStruct) ) );
    }

    private sealed class FormattableObject : IFormattable<TestRole>
    {
        public void Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
        {
            stringBuilder.Append( "Formattable" );
        }
    }

#if NET6_0_OR_GREATER
    private sealed class SpanFormattableObject : ISpanFormattable
    {
        // ToString should not be invoked.
        public string ToString( string? format, IFormatProvider? formatProvider ) => throw new NotImplementedException();

        public bool TryFormat( Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider )
        {
            const string str = "SpanFormattable";
            new Span<char>( str.ToCharArray() ).CopyTo( destination );
            charsWritten = str.Length;

            return true;
        }
    }
#endif

    private sealed class NullToStringClass
    {
        public override string? ToString()
        {
            return null;
        }
    }
}