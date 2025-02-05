// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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