// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class BooleanSerializer : IntrinsicSerializer<bool>
    {
        public override object Convert( object value, Type targetType )
        {
            return System.Convert.ToBoolean( value, CultureInfo.InvariantCulture );
        }
    }
}