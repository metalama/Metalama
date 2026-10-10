// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ProtectedBaseSerializerConstructor;

/*
 * The manual serializer of the base type has a protected parameterless constructor, which the generated serializer can call (#2168).
 */

[CompileTime]
public class BaseClass : ICompileTimeSerializable
{
    public class Serializer : ReferenceTypeSerializer
    {
        protected Serializer() { }

        public override object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new BaseClass();

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments ) { }
    }
}

//<target>
[CompileTime]
public class TargetClass : BaseClass { }
