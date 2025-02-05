// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.Introspection;

[PublicAPI]
public enum IntrospectionTransformationKind
{
    OverrideMember,
    InsertStatement,
    MakeDefaultConstructorExplicit,
    IntroduceAttribute,
    InsertConstructorInitializerArgument,
    IntroduceMember,
    ImplementInterface,
    IntroduceParameter,
    RemoveAttributes,
    AddAnnotation
}