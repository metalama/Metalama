// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Telemetry;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

public class NullTelemetryUploader : ITelemetryUploader
{
    public void StartUpload( bool force = false ) { }

    public Task UploadAsync() => Task.CompletedTask;
}