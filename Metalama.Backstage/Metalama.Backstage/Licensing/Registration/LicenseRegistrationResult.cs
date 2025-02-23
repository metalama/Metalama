// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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