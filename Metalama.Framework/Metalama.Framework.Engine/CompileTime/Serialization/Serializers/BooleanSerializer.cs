// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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