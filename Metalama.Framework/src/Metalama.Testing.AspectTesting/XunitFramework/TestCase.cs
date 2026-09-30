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
        /// <summary>
        /// The key of the serialized name of the test assembly.
        /// </summary>
        private const string _assemblyNameKey = "assemblyName";
        /// <summary>
        /// The key of the serialized relative path of the test file.
        /// </summary>
        private const string _relativePathKey = "relativePath";

        /// <summary>
        /// The factory of the test assembly, or <c>null</c> until the test case is initialized.
        /// </summary>
        private TestFactory? _factory;
        /// <summary>
        /// The path of the test file relative to the source directory, or <c>null</c> until the test case is initialized.
        /// </summary>
        private string? _relativePath;
        /// <summary>
        /// The test method of the test file, or <c>null</c> until the test case is initialized.
        /// </summary>
        private TestMethod? _testMethod;
        /// <summary>
        /// The reason why the test is skipped, read from the test file on first use.
        /// </summary>
        private Lazy<string?>? _skipReason;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class. This constructor is used by xunit, which
        /// calls <see cref="IXunitSerializable.Deserialize"/> next.
        /// </summary>
        [UsedImplicitly]
        public TestCase() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCase"/> class for a test file.
        /// </summary>
        public TestCase( TestFactory factory, string relativePath )
        {
            this.Initialize( factory, relativePath );
        }

        /// <summary>
        /// Sets the objects and the identifier that describe the test case.
        /// </summary>
        private void Initialize( TestFactory factory, string relativePath )
        {
            this._factory = factory;
            this._relativePath = relativePath;
            this._testMethod = factory.GetTestMethod( relativePath );
            this.UniqueID = UniqueIDGenerator.ForTestCase( this._testMethod.UniqueID, 0 );
            this._skipReason = new Lazy<string?>( this.GetSkipReason );
        }

        /// <summary>
        /// Gets the factory of the test assembly.
        /// </summary>
        private TestFactory Factory => this._factory ?? throw new InvalidOperationException( "The test case has not been initialized." );

        /// <summary>
        /// Gets the path of the test file, relative to the source directory of the test project.
        /// </summary>
        public string RelativePath => this._relativePath ?? throw new InvalidOperationException( "The test case has not been initialized." );

        /// <summary>
        /// Gets the full path of the test file.
        /// </summary>
        public string FullPath => Path.Combine( this.Factory.ProjectProperties.SourceDirectory, this.RelativePath );

        /// <inheritdoc />
        void IXunitSerializable.Deserialize( IXunitSerializationInfo info )
        {
            var assembly = Assembly.Load( info.GetValue<string>( _assemblyNameKey ).AssertNotNull() );
            var factory = TestFactory.GetInstance( TestFrameworkServiceFactoryProvider.GetServiceProvider, assembly );

            this.Initialize( factory, info.GetValue<string>( _relativePathKey ).AssertNotNull() );
        }

        /// <inheritdoc />
        void IXunitSerializable.Serialize( IXunitSerializationInfo info )
        {
            info.AddValue( _assemblyNameKey, this.Factory.Assembly.FullName );
            info.AddValue( _relativePathKey, this.RelativePath );
        }

        /// <inheritdoc />
        public bool Explicit => false;

        /// <summary>
        /// Gets the reason why the test is skipped, or <c>null</c> when it is not skipped.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A test is skipped by the <c>@Skipped</c> option of its test file, so the reason is known before the test runs,
        /// which is what xunit calls a static skip. The property never throws: it returns <c>null</c> when the test is not
        /// skipped, as <see cref="ITestCaseMetadata.SkipReason"/> requires.
        /// </para>
        /// <para>
        /// The reason is read from the test file the first time the property is read, and cached, because xunit reads the
        /// property several times per test.
        /// </para>
        /// </remarks>
        public string? SkipReason => this._skipReason?.Value;

        /// <summary>
        /// Reads the reason why the test is skipped from the test file.
        /// </summary>
        private string? GetSkipReason()
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

        /// <inheritdoc />
        public string? SourceFilePath => this.FullPath;

        /// <inheritdoc />
        public int? SourceLineNumber => 1;

        /// <inheritdoc />
        public string TestCaseDisplayName => Path.GetFileNameWithoutExtension( this.RelativePath );

        /// <inheritdoc />
        public int? TestClassMetadataToken => null;

        /// <inheritdoc />
        public string? TestClassName => this.TestClass.TestClassName;

        /// <inheritdoc />
        public string? TestClassNamespace => this.TestClass.TestClassNamespace;

        /// <inheritdoc />
        public string? TestClassSimpleName => this.TestClass.TestClassSimpleName;

        /// <inheritdoc />
        public int? TestMethodArity => null;

        /// <inheritdoc />
        public int? TestMethodMetadataToken => null;

        /// <inheritdoc />
        public string? TestMethodName => this.TestMethod.MethodName;

        /// <inheritdoc />
        public string[]? TestMethodParameterTypesVSTest => null;

        /// <inheritdoc />
        public string? TestMethodReturnTypeVSTest => null;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        /// <inheritdoc />
        public string UniqueID { get; private set; } = "";

        /// <inheritdoc />
        public ITestClass TestClass => this.TestMethod.TestClass;

        /// <inheritdoc />
        public ITestCollection TestCollection => this.TestClass.TestCollection;

        /// <inheritdoc />
        public ITestMethod TestMethod => this._testMethod ?? throw new InvalidOperationException( "The test case has not been initialized." );
    }
}
