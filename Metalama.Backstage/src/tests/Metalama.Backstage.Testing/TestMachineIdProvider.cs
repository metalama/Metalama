// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Infrastructure;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Reports a machine identifier chosen by the test instead of the identifier of the machine that runs the test, so
/// that a test can pin the value that the license audit hashes.
/// </summary>
[PublicAPI]
public sealed class TestMachineIdProvider : IMachineIdProvider
{
    /// <summary>
    /// The identifier reported unless the test sets <see cref="MachineId"/>. The value is arbitrary and has the shape
    /// of the <c>MachineGuid</c> value that Windows stores.
    /// </summary>
    public const string DefaultMachineId = "7f3a1c68-2b4e-4d19-9a5c-0e6b8d2f4a71";

    public string MachineId { get; set; } = DefaultMachineId;
}
