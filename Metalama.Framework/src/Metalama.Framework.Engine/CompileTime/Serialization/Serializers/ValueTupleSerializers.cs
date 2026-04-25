// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class ValueTupleSerializer : ValueTypeSerializer<ValueTuple>
{
    public override void SerializeObject( ValueTuple obj, IArgumentsWriter constructorArguments ) { }

    public override ValueTuple DeserializeObject( IArgumentsReader constructorArguments ) => default;
}

internal sealed class ValueTupleSerializer<T1> : ValueTypeSerializer<ValueTuple<T1>>
{
    public override void SerializeObject( ValueTuple<T1> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
    }

    public override ValueTuple<T1> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1>( constructorArguments.GetValue<T1>( "1" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2> : ValueTypeSerializer<ValueTuple<T1, T2>>
{
    public override void SerializeObject( ValueTuple<T1, T2> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
    }

    public override ValueTuple<T1, T2> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2, T3> : ValueTypeSerializer<ValueTuple<T1, T2, T3>>
{
    public override void SerializeObject( ValueTuple<T1, T2, T3> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
    }

    public override ValueTuple<T1, T2, T3> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2, T3, T4> : ValueTypeSerializer<ValueTuple<T1, T2, T3, T4>>
{
    public override void SerializeObject( ValueTuple<T1, T2, T3, T4> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
        constructorArguments.SetValue( "4", obj.Item4 );
    }

    public override ValueTuple<T1, T2, T3, T4> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3, T4>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )!,
            constructorArguments.GetValue<T4>( "4" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2, T3, T4, T5> : ValueTypeSerializer<ValueTuple<T1, T2, T3, T4, T5>>
{
    public override void SerializeObject( ValueTuple<T1, T2, T3, T4, T5> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
        constructorArguments.SetValue( "4", obj.Item4 );
        constructorArguments.SetValue( "5", obj.Item5 );
    }

    public override ValueTuple<T1, T2, T3, T4, T5> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3, T4, T5>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )!,
            constructorArguments.GetValue<T4>( "4" )!,
            constructorArguments.GetValue<T5>( "5" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2, T3, T4, T5, T6> : ValueTypeSerializer<ValueTuple<T1, T2, T3, T4, T5, T6>>
{
    public override void SerializeObject( ValueTuple<T1, T2, T3, T4, T5, T6> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
        constructorArguments.SetValue( "4", obj.Item4 );
        constructorArguments.SetValue( "5", obj.Item5 );
        constructorArguments.SetValue( "6", obj.Item6 );
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )!,
            constructorArguments.GetValue<T4>( "4" )!,
            constructorArguments.GetValue<T5>( "5" )!,
            constructorArguments.GetValue<T6>( "6" )! );
    }
}

internal sealed class ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7> : ValueTypeSerializer<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
{
    public override void SerializeObject( ValueTuple<T1, T2, T3, T4, T5, T6, T7> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
        constructorArguments.SetValue( "4", obj.Item4 );
        constructorArguments.SetValue( "5", obj.Item5 );
        constructorArguments.SetValue( "6", obj.Item6 );
        constructorArguments.SetValue( "7", obj.Item7 );
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6, T7> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )!,
            constructorArguments.GetValue<T4>( "4" )!,
            constructorArguments.GetValue<T5>( "5" )!,
            constructorArguments.GetValue<T6>( "6" )!,
            constructorArguments.GetValue<T7>( "7" )! );
    }
}

internal sealed class ValueTupleRestSerializer<T1, T2, T3, T4, T5, T6, T7, TRest> : ValueTypeSerializer<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
    where TRest : struct
{
    public override void SerializeObject( ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "1", obj.Item1 );
        constructorArguments.SetValue( "2", obj.Item2 );
        constructorArguments.SetValue( "3", obj.Item3 );
        constructorArguments.SetValue( "4", obj.Item4 );
        constructorArguments.SetValue( "5", obj.Item5 );
        constructorArguments.SetValue( "6", obj.Item6 );
        constructorArguments.SetValue( "7", obj.Item7 );
        constructorArguments.SetValue( "r", obj.Rest );
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> DeserializeObject( IArgumentsReader constructorArguments )
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
            constructorArguments.GetValue<T1>( "1" )!,
            constructorArguments.GetValue<T2>( "2" )!,
            constructorArguments.GetValue<T3>( "3" )!,
            constructorArguments.GetValue<T4>( "4" )!,
            constructorArguments.GetValue<T5>( "5" )!,
            constructorArguments.GetValue<T6>( "6" )!,
            constructorArguments.GetValue<T7>( "7" )!,
            constructorArguments.GetValue<TRest>( "r" ) );
    }
}