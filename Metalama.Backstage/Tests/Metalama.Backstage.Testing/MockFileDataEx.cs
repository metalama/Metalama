// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace Metalama.Backstage.Testing
{
    [PublicAPI]
    public class MockFileDataEx : MockFileData
    {
        public MockFileDataEx( string textContents )
            : base( textContents ) { }

        public MockFileDataEx( byte[] contents )
            : base( contents ) { }

        public MockFileDataEx( MockFileData template )
            : base( template ) { }

        public MockFileDataEx( string textContents, Encoding encoding )
            : base( textContents, encoding ) { }

        public MockFileDataEx( params string[] content )
            : this( string.Join( Environment.NewLine, content ) ) { }
    }
}