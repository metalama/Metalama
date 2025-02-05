// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;

internal interface ITestAssemblyMetadataReader : IGlobalService
{
    TestAssemblyMetadata GetMetadata( IAssemblyInfo assembly );
}