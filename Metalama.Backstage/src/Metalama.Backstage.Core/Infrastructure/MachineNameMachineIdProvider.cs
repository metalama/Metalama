// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Reports <see cref="Environment.MachineName"/> as the identifier of the machine. This implementation serves the
/// operating systems for which we know no better identifier.
/// </summary>
/// <remarks>
/// The machine name is stable, but it is not guaranteed to be unique, so a device count that includes such a machine
/// is a lower bound. See issue #1873.
/// </remarks>
internal sealed class MachineNameMachineIdProvider : MachineIdProvider
{
    public MachineNameMachineIdProvider( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override string? ReadMachineId() => Environment.MachineName;
}
