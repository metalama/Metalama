// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.CodeModel.References
{
    internal enum RefTargetKind
    {
        // WARNING! These values are serialized as strings and stored in compiled dlls. Do not rename.

        Default,
        Return,
        Assembly,
        Module,
        Field,
        Parameter,
        Property,
        Event,
        PropertyGet,
        PropertySet,
        StaticConstructor,
        PropertySetParameter,
        PropertyGetReturnParameter,
        PropertySetReturnParameter,
        EventRaise,
        EventRaiseParameter,
        EventRaiseReturnParameter,
        NamedType
    }
}