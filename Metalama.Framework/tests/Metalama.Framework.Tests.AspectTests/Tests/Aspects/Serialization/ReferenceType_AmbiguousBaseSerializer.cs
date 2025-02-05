using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_AmbiguousBaseSerializer;

/*
 * The base serializer is ambiguous.
 */

[CompileTime]
public class BaseClass : ICompileTimeSerializable
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

//<target>
public class TargetClass : BaseClass { }