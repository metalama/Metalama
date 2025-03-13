// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Licensing.Registration;

public readonly struct LicenseRegistrationResult
{
    private readonly LicenseRegistrationProperties? _registeredLicense;
    private readonly string? _errorMessage;

    public LicenseRegistrationProperties RegisteredLicense => this._registeredLicense ?? throw new InvalidOperationException();

    public string ErrorMessage => this._errorMessage ?? throw new InvalidOperationException();

    public bool IsSuccess => this._registeredLicense != null;

    private LicenseRegistrationResult( LicenseRegistrationProperties? registeredLicense, string? errorMessage )
    {
        this._registeredLicense = registeredLicense;
        this._errorMessage = errorMessage;
    }

    public static LicenseRegistrationResult Success( LicenseRegistrationProperties license ) => new( license, null );

    public static LicenseRegistrationResult Failure( string errorMessage ) => new( null, errorMessage );
}