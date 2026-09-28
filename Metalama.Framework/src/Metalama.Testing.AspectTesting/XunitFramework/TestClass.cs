// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.IO;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// The test class that represents a directory of test files.
    /// </summary>
    internal sealed class TestClass : ITestClass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestClass"/> class for a directory of test files.
        /// </summary>
        public TestClass( TestFactory factory, string relativePath )
        {
            this.TestCollection = factory.TestCollection;

            // If the directory contains both files and subdirectories, we have to generate a class name with a "Tests" suffix.

            var directory = factory.ProjectProperties.SourceDirectory;

            if ( !string.IsNullOrEmpty( relativePath ) )
            {
                directory = Path.Combine( directory, relativePath );
            }

            string name;

            if ( factory.FileSystem.GetDirectories( directory ).Length > 0 && factory.FileSystem.GetFiles( directory ).Length > 0 )
            {
                name = string.IsNullOrEmpty( relativePath ) ? "Tests" : relativePath.Replace( Path.DirectorySeparatorChar, '.' ) + ".Tests";
            }
            else
            {
                name = string.IsNullOrEmpty( relativePath ) ? "Tests" : relativePath.Replace( Path.DirectorySeparatorChar, '.' );
            }

            this.TestClassName = factory.ProjectName + "." + name;

            var lastDot = this.TestClassName.LastIndexOf( '.' );
            this.TestClassNamespace = lastDot < 0 ? null : this.TestClassName.Substring( 0, lastDot );
            this.TestClassSimpleName = lastDot < 0 ? this.TestClassName : this.TestClassName.Substring( lastDot + 1 );

            this.UniqueID = UniqueIDGenerator.ForTestClass( this.TestCollection.UniqueID, this.TestClassName );
        }

        /// <inheritdoc />
        public ITestCollection TestCollection { get; }

        /// <inheritdoc />
        public string TestClassName { get; }

        /// <inheritdoc />
        public string? TestClassNamespace { get; }

        /// <inheritdoc />
        public string TestClassSimpleName { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        /// <inheritdoc />
        public string UniqueID { get; }
    }
}
