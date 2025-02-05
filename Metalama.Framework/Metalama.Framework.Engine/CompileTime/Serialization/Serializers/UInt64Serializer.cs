// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class UInt64Serializer : IntrinsicSerializer<ulong>
    {
        public override object Convert( object value, Type targetType )
        {
            return System.Convert.ToUInt64( value, CultureInfo.InvariantCulture );
        }
    }
}