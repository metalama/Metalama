// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry.User;

public interface IUserInfoService : IBackstageService
{
    bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo );
    
    void SaveEmailAddress( string emailAddress );
}