# Requirements

## Completed

* Property chains of type `A.B.C....D` or `A?.B?.C....?D` (or a combination of `?.` or `.`) where all types implement `INotifyPropertyChanged<T>` and all nodes are properties (not fields) in the same project.
* Report warning on unsupported constructs.
* `[IgnoreUnobservableExpressions]` suppresses the warning.
* Derived types inheriting from a a base type which is enhanced by `[Observable]`.

* Support for "legacy" `INotifyPropertyChanged`.
* API for human-generated hints
* 
## Next

* Support named events e.g. WPF-style `ColorChanged`.
* Support for functions
* Cross-project
