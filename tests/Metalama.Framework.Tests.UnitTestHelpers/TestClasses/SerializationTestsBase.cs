// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System.IO;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public class SerializationTestsBase
{
    protected static T Roundloop<T>( T input )
    {
        var stringWriter = new StringWriter();
        var serializer = JsonSerializer.Create( new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All } );
        serializer.Serialize( stringWriter, input );

        return serializer.Deserialize<T>( new JsonTextReader( new StringReader( stringWriter.ToString() ) ) )!;
    }
}