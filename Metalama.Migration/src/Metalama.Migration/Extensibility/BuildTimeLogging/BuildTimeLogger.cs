// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace PostSharp.Extensibility.BuildTimeLogging
{
    public sealed partial class BuildTimeLogger
    {
        public void WriteLine( string message )
        {
            throw new NotImplementedException();
        }

        public void WriteLine( string message, object[] args )
        {
            throw new NotImplementedException();
        }

        public void Write( string message )
        {
            throw new NotImplementedException();
        }

        public void Write( string message, object[] args )
        {
            throw new NotImplementedException();
        }

        public BuildTimeLogActivity Activity( string message )
        {
            throw new NotImplementedException();
        }

        public static BuildTimeLogger GetInstance( string category )
        {
            throw new NotImplementedException();
        }

        public static void Initialize( IEnumerable<string> enabledCategories = null ) => throw new NotImplementedException();

        public static bool IsInitialized => throw new NotImplementedException();
    }
}