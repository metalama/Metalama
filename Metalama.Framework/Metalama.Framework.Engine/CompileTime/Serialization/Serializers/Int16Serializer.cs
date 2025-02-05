// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class Int16Serializer : IntrinsicSerializer<short>
    {
        public override object Convert( object value, Type targetType )
        {
            return System.Convert.ToInt16( value, CultureInfo.InvariantCulture );
        }
    }
}