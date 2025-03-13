// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Metalama.Patterns.Observability.AspectTests.WriteabilityNonInpc;

[Observable]
public class PersonViewModelField
{
    private string _name;
    private DateOnly _dateOfBirth;

    public PersonViewModelField( string name, DateOnly dateOfBirth )
    {
        this._name = name;
        this._dateOfBirth = dateOfBirth;
    }

    public string Name => this._name;

    public DateOnly DateOfBirth => this._dateOfBirth;

    public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
}

[Observable]
public class PersonViewModelReadonlyField
{
    private readonly string _name;
    private readonly DateOnly _dateOfBirth;

    public PersonViewModelReadonlyField( string name, DateOnly dateOfBirth )
    {
        this._name = name;
        this._dateOfBirth = dateOfBirth;
    }

    public string Name => this._name;

    public DateOnly DateOfBirth => this._dateOfBirth;

    public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
}

[Observable]
public class PersonViewModelConst
{
    private const string _name = "Adele";
    private const int _yearOfBirth = 1988;

    public string Name => _name;

    public int YearOfBirth => _yearOfBirth;

    public string Description => $"{this.Name} (b. {this.YearOfBirth})";
}

[Observable]
public class PersonViewModelGetOnlyProperty
{
    private string Name0 { get; }

    private DateOnly DateOfBirth0 { get; }

    public PersonViewModelGetOnlyProperty( string name, DateOnly dateOfBirth )
    {
        this.Name0 = name;
        this.DateOfBirth0 = dateOfBirth;
    }

    public string Name => this.Name0;

    public DateOnly DateOfBirth => this.DateOfBirth0;

    public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
}

[Observable]
public class PersonViewModelInitProperty
{
    private string Name0 { get; init; }

    private DateOnly DateOfBirth0 { get; init; }

    public PersonViewModelInitProperty( string name, DateOnly dateOfBirth )
    {
        this.Name0 = name;
        this.DateOfBirth0 = dateOfBirth;
    }

    public string Name => this.Name0;

    public DateOnly DateOfBirth => this.DateOfBirth0;

    public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
}

[Observable]
public class PersonViewModelProperty
{
    private string Name0 { get; set; }

    private DateOnly DateOfBirth0 { get; set; }

    public PersonViewModelProperty( string name, DateOnly dateOfBirth )
    {
        this.Name0 = name;
        this.DateOfBirth0 = dateOfBirth;
    }

    public string Name => this.Name0;

    public DateOnly DateOfBirth => this.DateOfBirth0;

    public string Description => $"{this.Name} (b. {this.DateOfBirth.Year})";
}