// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CompileTime;

internal interface ITemplateInfo
{
    SerializableDeclarationId Id { get; }

    bool IsAbstract { get; }

    bool HasNoBody { get; }

    TemplateAttributeType AttributeType { get; }

    bool IsNone { get; }

    RoslynApiVersion? UsedApiVersion { get; }
}