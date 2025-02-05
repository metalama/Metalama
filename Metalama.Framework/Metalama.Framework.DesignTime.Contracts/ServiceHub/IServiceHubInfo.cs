// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.ServiceHub;

[ComImport]
[Guid( "A3F04A92-6ECA-4861-956A-57AD6309C095" )]
public interface IServiceHubInfo
{
    string PipeName { get; }

    Version Version { get; }
}