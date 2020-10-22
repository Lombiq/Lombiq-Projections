# Lombiq Projections Orchard module readme



## About

An [Orchard CMS](http://orchardproject.net/) module that adds some useful Projection filters:

- `Lombiq Projections` feature:
    - `NullSafeContentFieldsFilter`: Essentially the same as `ContentFieldsFilter` from the `Orchard.Projections` module, except that it won't exclude items that are indexed as `NULL` in the corresponding field index table, e.g. `StringFieldIndexRecord` for `TextField` and `InputField`. Indexing a field with `NULL` value happens when the type of the object storing the value of field is nullable and the value is empty.
    - `TokenizedContentTypeFilter`: The same as `ContentTypeFilter` from the `Orchard.Projections` module, except that the filtering value is tokenized, instead of having to select one or more specific types.
- `Lombiq Projections - Fields` feature:
    - `TokenizedBooleanFieldFilter`: The same as `ContentFieldFilter` for `BooleanField` from the `Orchard.Projections` module, except that the filtering value is tokenized.
- `Lombiq Projections - Taxonomies` feature:
    - `TokenizedTaxonomyTermsFilter`: The `TermsFilter` from `Orchard.Taxonomies` on steroids: Much more flexible due to the additional options, like tokenization, inverting filter results and selectable property of the Terms to match.


## Contributing and support

Bug reports, feature requests, comments, questions, code contributions, and love letters are warmly welcome, please do so via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.