// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

internal sealed class TestAssemblyInfo : LongLivedMarshalByRefObject, IAssemblyInfo
{
    public TestAssemblyInfo( string assemblyPath )
    {
        this.AssemblyPath = assemblyPath;
    }

    public IEnumerable<IAttributeInfo> GetCustomAttributes( string assemblyQualifiedAttributeTypeName ) => Enumerable.Empty<IAttributeInfo>();

    public ITypeInfo GetType( string typeName ) => throw new NotSupportedException();

    public IEnumerable<ITypeInfo> GetTypes( bool includePrivateTypes ) => Enumerable.Empty<ITypeInfo>();

    public string AssemblyPath { get; }

    public string Name => Path.GetFileNameWithoutExtension( this.AssemblyPath );
}