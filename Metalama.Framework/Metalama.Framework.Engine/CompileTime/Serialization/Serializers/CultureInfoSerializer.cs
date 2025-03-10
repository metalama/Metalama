// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    // This needs to be public because the type is instantiated from an activator in client assemblies.

    /// <exclude/>
    internal sealed class CultureInfoSerializer : ReferenceTypeSerializer
    {
        /// <exclude/>
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
#if CULTURE_INFO_USER_OVERRIDE
            if (constructorArguments.TryGetValue( "useUserOverride", out bool useUserOverride ))
                return new CultureInfo( constructorArguments.GetValue<string>( "identifier" ), useUserOverride );
#endif

            // This is returned if we're running .NETStandard1.3 or useUserOverride was not set.
            return new CultureInfo( constructorArguments.GetValue<string>( "identifier" )! );
        }

        /// <exclude/>
        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var info = (CultureInfo) obj;
            constructorArguments.SetValue( "identifier", info.Name );
            constructorArguments.SetValue( "useUserOverride", info.UseUserOverride );
        }

        /// <exclude/>
        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }
    }
}