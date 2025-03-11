// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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