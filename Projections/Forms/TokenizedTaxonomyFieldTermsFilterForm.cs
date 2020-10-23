using Lombiq.Projections.Constants;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Taxonomies.Models;
using System;

namespace Lombiq.Projections.Projections.Forms
{
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TokenizedTaxonomyFieldTermsFilterForm : TokenizedValueListFilterFormBase
    {
        private readonly dynamic _shapeFactory;


        public TokenizedTaxonomyFieldTermsFilterForm(IShapeFactory shapeFactory) : base(shapeFactory)
        {
            _shapeFactory = shapeFactory;
        }


        public override void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedTaxonomyFieldTermsFilterForm), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedTaxonomyFieldTermsFilterForm),
                    _ContainsOrNot: _shapeFactory.FieldSet(
                        _Contains: _shapeFactory.Radio(
                            Id: "contains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that has"), Value: "true", Checked: true),
                        _NotContains: _shapeFactory.Radio(
                            Id: "notContains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that doesn't have"), Value: "false")),
                    _Relationship: GetFilterRelationshipTextbox(),
                    _Property: _shapeFactory.FieldSet(
                        _PropertyName: _shapeFactory.Radio(
                            Id: "termPropertyName", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.TermProperty),
                            Title: T("Defined by the Name"), Value: nameof(TermPart.Name), Checked: true),
                        _PropertyId: _shapeFactory.Radio(
                            Id: "termPropertyId", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.TermProperty),
                            Title: T("Defined by the Id"), Value: nameof(TermPart.Id))),
                    _Terms: _shapeFactory.Textbox(
                        Id: "terms", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Terms),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Terms"),
                        Description: T("The comma-separated list of Taxonomy Terms (based on their property selected above) to filter content items with."))));
    }


    public class TokenizedTaxonomyFieldTermsFilterFormElements : TokenizedValueListFilterFormElementsBase
    {
        public int Operator { get; set; }
        public string TermProperty { get; set; }
        public string Terms { get; set; }
        public bool Contains { get; set; }


        public TokenizedTaxonomyFieldTermsFilterFormElements(object formState) : base(formState)
        {
            TermProperty = FormState[nameof(TermProperty)];
            Terms = FormState[nameof(Terms)];
            Matches = Contains = FormState[nameof(Contains)] ?? false;

            // Backwards-compatibility with filter forms saved before the filter relationship became a tokenized textbox.
            if (string.IsNullOrEmpty(FilterRelationshipString))
                Operator = string.IsNullOrEmpty(FormState[nameof(Operator)]?.Value) ? 0 : int.Parse(FormState[nameof(Operator)].Value);
            else Operator = FilterRelationship == ValueFilterRelationship.Or ? 0 : 1;
        }


        public override Action<IHqlExpressionFactory> GetFilterExpression(string property, string value = "") =>
            base.GetFilterExpression(property, value);
    }
}