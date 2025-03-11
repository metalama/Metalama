// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Metalama.Patterns.Observability.AspectTests.WriteabilityInpc;

[Observable]
public class Person
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

[Observable]
public class PersonViewModelField
{
    private Person _model;

    public PersonViewModelField( Person model )
    {
        this._model = model;
    }

    public string? FirstName => this._model.FirstName;

    public string? LastName => this._model.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}

[Observable]
public class PersonViewModelReadonlyField
{
    private readonly Person _model;

    public PersonViewModelReadonlyField( Person model )
    {
        this._model = model;
    }

    public string? FirstName => this._model.FirstName;

    public string? LastName => this._model.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}

[Observable]
public class PersonViewModelConst
{
    private const Person? _model = null;

    public string? FirstName => _model?.FirstName;

    public string? LastName => _model?.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}

[Observable]
public class PersonViewModelGetOnlyProperty
{
    private Person Model { get; }

    public PersonViewModelGetOnlyProperty( Person model )
    {
        this.Model = model;
    }

    public string? FirstName => this.Model.FirstName;

    public string? LastName => this.Model.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}

[Observable]
public class PersonViewModelInitProperty
{
    private Person Model { get; init; }

    public PersonViewModelInitProperty( Person model )
    {
        this.Model = model;
    }

    public string? FirstName => this.Model.FirstName;

    public string? LastName => this.Model.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}

[Observable]
public class PersonViewModelProperty
{
    private Person Model { get; set; }

    public PersonViewModelProperty( Person model )
    {
        this.Model = model;
    }

    public string? FirstName => this.Model.FirstName;

    public string? LastName => this.Model.LastName;

    public string FullName => $"{this.FirstName} {this.LastName}";
}