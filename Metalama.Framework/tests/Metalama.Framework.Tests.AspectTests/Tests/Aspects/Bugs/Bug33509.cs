using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33509;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Double, ConversionKind.Implicit )}" );
        Console.WriteLine( $"Should be false: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Double, ConversionKind.Default )}" );
        Console.WriteLine( $"Should be false: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Double, ConversionKind.Reference )}" );

        Console.WriteLine(
            $"Should be false: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Double, ConversionKind.TypeDefinition )}" );

        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Object, ConversionKind.Implicit )}" );
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Object, ConversionKind.Default )}" );
        Console.WriteLine( $"Should be false: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Object, ConversionKind.Reference )}" );
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Int32 ).IsConvertibleTo( SpecialType.Object, ConversionKind.TypeDefinition )}" );

        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Task ).IsConvertibleTo( SpecialType.Object, ConversionKind.Implicit )}" );
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Task ).IsConvertibleTo( SpecialType.Object, ConversionKind.Default )}" );
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Task ).IsConvertibleTo( SpecialType.Object, ConversionKind.Reference )}" );
        Console.WriteLine( $"Should be true: {TypeFactory.GetType( SpecialType.Task ).IsConvertibleTo( SpecialType.Object, ConversionKind.TypeDefinition )}" );

        return meta.Proceed();
    }
}

// <target>
public partial class TestClass
{
    [TestAspect]
    public void Foo() { }
}