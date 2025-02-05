// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Worker.Upload;

// The settings class is required even when it's empty, because the base class is abstract,
// and Spectre attempts to instantiate it in run time.
[UsedImplicitly]
internal class UploadCommandSettings : CommandSettings;