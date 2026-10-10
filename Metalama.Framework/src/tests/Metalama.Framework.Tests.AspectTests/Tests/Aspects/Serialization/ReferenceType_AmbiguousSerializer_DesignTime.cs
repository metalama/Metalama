// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_AmbiguousSerializer_DesignTime;

/*
 * The serializer of a type without derived types is ambiguous. At design time, the pipeline must report LAMA0208 instead of throwing an exception.
 */

[CompileTime]
public class SerializableClass : ICompileTimeSerializable
{
    public class Serializer1 : ISerializer
    {
        public bool IsTwoPhase => throw new NotImplementedException();

        public object Convert( object value, Type targetType ) => throw new NotImplementedException();

        public object CreateInstance( Type type, IArgumentsReader constructorArguments ) => throw new NotImplementedException();

        public void DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) => throw new NotImplementedException();

        public void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => throw new NotImplementedException();
    }

    public class Serializer2 : ISerializer
    {
        public bool IsTwoPhase => throw new NotImplementedException();

        public object Convert( object value, Type targetType ) => throw new NotImplementedException();

        public object CreateInstance( Type type, IArgumentsReader constructorArguments ) => throw new NotImplementedException();

        public void DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) => throw new NotImplementedException();

        public void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => throw new NotImplementedException();
    }
}
