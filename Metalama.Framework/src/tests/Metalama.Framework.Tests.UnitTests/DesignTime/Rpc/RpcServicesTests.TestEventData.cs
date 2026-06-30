// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServicesTests
{
    private protected override IEnumerable<Type> AdditionalRpcContractTypes => [typeof(TestEventData)];

    [JsonObject]
    private sealed class TestEventData : RpcEventData
    {
        public override string Category => "Test";

        public string Message { get; }

        public TestEventData( string message )
        {
            this.Message = message;
        }
    }
}
