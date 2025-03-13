// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.InheritableTypeFabric_CrossAssembly
{

     // <target>
    internal class DerivedClass : BaseClass
    {
        private int Method3( int a ) => a;

        private string Method4( string s ) => s;
    }
}