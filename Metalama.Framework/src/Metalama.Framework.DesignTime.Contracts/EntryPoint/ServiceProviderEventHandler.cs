// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

[Guid( "73840882-EFA8-4EC8-93A3-5673567C204D" )]
public delegate void ServiceProviderEventHandler( ICompilerServiceProvider serviceProvider );