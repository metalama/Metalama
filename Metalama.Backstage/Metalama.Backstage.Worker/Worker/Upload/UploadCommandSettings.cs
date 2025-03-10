// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Worker.Upload;

// The settings class is required even when it's empty, because the base class is abstract,
// and Spectre attempts to instantiate it in run time.
[UsedImplicitly]
internal class UploadCommandSettings : CommandSettings;