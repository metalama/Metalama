// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// The test case that represents a test file.
    /// </summary>
    internal sealed class TestCase : ITestCase, IXunitSerializable
    {
        private const string _assemblyNameKey = "assemblyName";
        private const string _relativePathKey = "relativePath";

        private TestFactory? _factory;
        private string? _relativePath;
        private TestMethod? _testMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class. This constructor is used by xunit, which
        /// calls <see cref="IXunitSerializable.Deserialize"/> next.
        /// </summary>
        [UsedImplicitly]
        public TestCase() { }

        public TestCase( TestFactory factory, string relativePath )
        {
            this.Initialize( factory, relativePath );
        }

        private void Initialize( TestFactory factory, string relativePath )
        {
            this._factory = factory;
            this._relativePath = relativePath;
            this._testMethod = factory.GetTestMethod( relativePath );
            this.UniqueID = UniqueIDGenerator.ForTestCase( this._testMethod.UniqueID, 0 );
        }

        private TestFactory Factory => this._factory ?? throw new InvalidOperationException( "The test case has not been initialized." );

        /// <summary>
        /// Gets the path of the test file, relative to the source directory of the test project.
        /// </summary>
        public string RelativePath => this._relativePath ?? throw new InvalidOperationException( "The test case has not been initialized." );

        public string FullPath => Path.Combine( this.Factory.ProjectProperties.SourceDirectory, this.RelativePath );

        void IXunitSerializable.Deserialize( IXunitSerializationInfo info )
        {
            var assembly = Assembly.Load( info.GetValue<string>( _assemblyNameKey ).AssertNotNull() );
            var factory = TestFactory.GetInstance( TestFrameworkServiceFactoryProvider.GetServiceProvider(), assembly );

            this.Initialize( factory, info.GetValue<string>( _relativePathKey ).AssertNotNull() );
        }

        void IXunitSerializable.Serialize( IXunitSerializationInfo info )
        {
            info.AddValue( _assemblyNameKey, this.Factory.Assembly.FullName );
            info.AddValue( _relativePathKey, this.RelativePath );
        }

        public bool Explicit => false;

        public string? SkipReason
        {
            get
            {
                try
                {
                    return this.Factory.TestInputFactory.FromFile( this.Factory.ProjectProperties, this.Factory.DirectoryOptionsReader, this.RelativePath )
                        .SkipReason;
                }
                catch ( Exception )
                {
                    // We want an exception here to be reported, so we cannot skip the test in this case.
                    return null;
                }
            }
        }

        public string? SourceFilePath => this.FullPath;

        public int? SourceLineNumber => 1;

        public string TestCaseDisplayName => Path.GetFileNameWithoutExtension( this.RelativePath );

        public int? TestClassMetadataToken => null;

        public string? TestClassName => this.TestClass.TestClassName;

        public string? TestClassNamespace => this.TestClass.TestClassNamespace;

        public string? TestClassSimpleName => this.TestClass.TestClassSimpleName;

        public int? TestMethodArity => null;

        public int? TestMethodMetadataToken => null;

        public string? TestMethodName => this.TestMethod.MethodName;

        public string[]? TestMethodParameterTypesVSTest => null;

        public string? TestMethodReturnTypeVSTest => null;

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        public string UniqueID { get; private set; } = "";

        public ITestClass TestClass => this.TestMethod.TestClass;

        public ITestCollection TestCollection => this.TestClass.TestCollection;

        public ITestMethod TestMethod => this._testMethod ?? throw new InvalidOperationException( "The test case has not been initialized." );
    }
}
