// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassNotSerializable_NoCtorError;

/*
 * The base class of a serializable typeis not itself serializable and does not have a parameterless base constructor.
 */

[RunTimeOrCompileTime]
public class BaseType
{
    public int BaseValue { get; }

    public BaseType( int baseValue )
    {
        BaseValue = 13;
    }
}

[RunTimeOrCompileTime]
public class DerivedType : BaseType, ICompileTimeSerializable
{
    public int Value { get; }

    public DerivedType( int value, int baseValue ) : base( baseValue )
    {
        Value = value;
    }
}