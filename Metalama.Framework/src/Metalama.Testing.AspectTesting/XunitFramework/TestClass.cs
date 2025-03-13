// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestClass : ITypeInfo, ITestClass
    {
        private readonly TestFactory _testFactory;
        private readonly string _name;

        public TestClass( TestFactory factory, string? relativePath )
        {
            this._testFactory = factory;

            // If the directory contains both files and subdirectories, we have to generate a class name with a "Tests" suffix.

            var directory = factory.ProjectProperties.SourceDirectory;

            if ( !string.IsNullOrEmpty( relativePath ) )
            {
                directory = Path.Combine( directory, relativePath );
            }
            else
            {
                // When compiling with the .NET Framework, 'relativePath' is not recognized as not null.
                Assert.NotNull( relativePath );
            }

            if ( this._testFactory.FileSystem.GetDirectories( directory ).Length > 0 && this._testFactory.FileSystem.GetFiles( directory ).Length > 0 )
            {
                if ( string.IsNullOrEmpty( relativePath ) )
                {
                    this._name = "Tests";
                }
                else
                {
                    this._name = relativePath.NotNull().Replace( Path.DirectorySeparatorChar, '.' ) + ".Tests";
                }
            }
            else
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                this._name = relativePath?.Replace( Path.DirectorySeparatorChar, '.' ) ?? "Tests";
            }

            this._name = factory.ProjectName + "." + this._name;
        }

        IEnumerable<IAttributeInfo> ITypeInfo.GetCustomAttributes( string assemblyQualifiedAttributeTypeName ) => Enumerable.Empty<IAttributeInfo>();

        IEnumerable<ITypeInfo> ITypeInfo.GetGenericArguments() => Enumerable.Empty<ITypeInfo>();

        IMethodInfo ITypeInfo.GetMethod( string methodName, bool includePrivateMethod ) => throw new NotImplementedException();

        IEnumerable<IMethodInfo> ITypeInfo.GetMethods( bool includePrivateMethods ) => throw new NotImplementedException();

        IAssemblyInfo ITypeInfo.Assembly => this._testFactory.AssemblyInfo;

        ITypeInfo ITypeInfo.BaseType => null!;

        IEnumerable<ITypeInfo> ITypeInfo.Interfaces => Enumerable.Empty<ITypeInfo>();

        bool ITypeInfo.IsAbstract => false;

        bool ITypeInfo.IsGenericParameter => false;

        bool ITypeInfo.IsGenericType => false;

        bool ITypeInfo.IsSealed => false;

        bool ITypeInfo.IsValueType => false;

        string ITypeInfo.Name => this._name;

        void IXunitSerializable.Deserialize( IXunitSerializationInfo info ) => throw new NotImplementedException();

        void IXunitSerializable.Serialize( IXunitSerializationInfo info ) => throw new NotImplementedException();

        ITypeInfo ITestClass.Class => this;

        ITestCollection ITestClass.TestCollection => this._testFactory.TestCollection;
    }
}