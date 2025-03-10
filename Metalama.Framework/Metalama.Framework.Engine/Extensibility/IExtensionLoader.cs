// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

public interface IExtensionLoader : IGlobalService
{
    IEnumerable<Type> GetExtensionTypes( IProjectOptions projectOptions, CompileTimeDomain domain, ExtensionKind extensionKind );
}