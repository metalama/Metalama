// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestCase : LongLivedMarshalByRefObject, ITestCase, ISourceInformation
    {
        private TestFactory _factory;
        private string _relativePath;

        public TestCase( TestFactory factory, string relativePath )
        {
            this._factory = factory;
            this._relativePath = relativePath;
        }

        public string FullPath => Path.Combine( this._factory.ProjectProperties.SourceDirectory, this._relativePath );

        void IXunitSerializable.Deserialize( IXunitSerializationInfo info )
        {
            this._factory = new TestFactory(
                this._factory.ServiceProvider,
                this._factory.ProjectProperties,
                info.GetValue<string>( "basePath" ),
                info.GetValue<string>( "assemblyName" ) );

            this._relativePath = info.GetValue<string>( "relativePath" );
        }

        void IXunitSerializable.Serialize( IXunitSerializationInfo info )
        {
            info.AddValue( "basePath", this._factory.ProjectProperties );
            info.AddValue( "relativePath", this._relativePath );
            info.AddValue( "assemblyName", this._factory.AssemblyInfo.Name );
        }

        string ITestCase.DisplayName => Path.GetFileNameWithoutExtension( this._relativePath );

        public string? SkipReason
        {
            get
            {
                try
                {
                    return this._factory.TestInputFactory.FromFile( this._factory.ProjectProperties, this._factory.DirectoryOptionsReader, this._relativePath )
                        .SkipReason;
                }
                catch ( Exception )
                {
                    // We want an exception here to be reported, so we cannot skip the test in this case.
                    return null;
                }
            }
        }

        ISourceInformation ITestCase.SourceInformation
        {
            get => this;
            set => throw new NotSupportedException();
        }

        Dictionary<string, List<string>> ITestCase.Traits { get; } = new();

        string ITestCase.UniqueID => this._relativePath;

        ITestMethod ITestCase.TestMethod => this._factory.GetTestMethod( this._relativePath );

        object[] ITestCase.TestMethodArguments => Array.Empty<object>();

        string ISourceInformation.FileName
        {
            get => Path.Combine( this._factory.ProjectProperties.SourceDirectory, this._relativePath );
            set => throw new NotSupportedException();
        }

        int? ISourceInformation.LineNumber
        {
            get => 1;
            set => throw new NotSupportedException();
        }
    }
}