// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry.User;

internal abstract class UserInfoSource
{
    public abstract bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo );
}