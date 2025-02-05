// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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