// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestMethod : LongLivedMarshalByRefObject, ITestMethod, IMethodInfo
    {
        private readonly TestFactory _factory;
        private readonly string _relativePath;

        public TestMethod( TestFactory factory, string relativePath )
        {
            this._factory = factory;
            this._relativePath = relativePath;
        }

        void IXunitSerializable.Deserialize( IXunitSerializationInfo info ) => throw new NotImplementedException();

        void IXunitSerializable.Serialize( IXunitSerializationInfo info ) => throw new NotImplementedException();

        IMethodInfo ITestMethod.Method => this;

        ITestClass ITestMethod.TestClass => this._factory.GetTestType( Path.GetDirectoryName( this._relativePath ) );

        IEnumerable<IAttributeInfo> IMethodInfo.GetCustomAttributes( string assemblyQualifiedAttributeTypeName ) => Enumerable.Empty<IAttributeInfo>();

        IEnumerable<ITypeInfo> IMethodInfo.GetGenericArguments() => Enumerable.Empty<ITypeInfo>();

        IEnumerable<IParameterInfo> IMethodInfo.GetParameters() => Enumerable.Empty<IParameterInfo>();

        IMethodInfo IMethodInfo.MakeGenericMethod( params ITypeInfo[] typeArguments ) => throw new NotSupportedException();

        bool IMethodInfo.IsAbstract => false;

        bool IMethodInfo.IsGenericMethodDefinition => false;

        bool IMethodInfo.IsPublic => true;

        bool IMethodInfo.IsStatic => true;

        string IMethodInfo.Name => Path.GetFileNameWithoutExtension( this._relativePath );

        ITypeInfo IMethodInfo.ReturnType => new ReflectionTypeInfo( typeof(void) );

        ITypeInfo IMethodInfo.Type => this._factory.GetTestType( Path.GetDirectoryName( this._relativePath )! );
    }
}