// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

[Guid( "A774931E-EF64-44D0-BD02-957BD60B3CCF" )]
public delegate void ServiceProviderEventHandler( ICompilerServiceProvider serviceProvider );