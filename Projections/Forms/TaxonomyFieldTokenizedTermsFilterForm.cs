using Lombiq.Projections.Constants;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Taxonomies.Models;

namespace Lombiq.Projections.Projections.Forms
{
    internal class TaxonomyFieldTokenizedTermsFilterFormElements
    {
        public int Operator { get; set; }
        public string TermProperty { get; set; }
        public string Terms { get; set; }
        public bool Contains { get; set; }


        public TaxonomyFieldTokenizedTermsFilterFormElements(dynamic formState)
        {
            Operator = string.IsNullOrEmpty(formState[nameof(Operator)]?.Value) ? 0 : int.Parse(formState[nameof(Operator)].Value);
            TermProperty = formState[nameof(TermProperty)];
            Terms = formState[nameof(Terms)];
            Contains = formState[nameof(Contains)] ?? false;
        }
    }

    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TaxonomyFieldTokenizedTermsFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }

        public static string FormName = nameof(TaxonomyFieldTokenizedTermsFilterForm);


        public TaxonomyFieldTokenizedTermsFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context) =>
            context.Form(FormName, shape =>
                _shapeFactory.Form(
                    Id: FormName,
                    _ContainsOrNot: _shapeFactory.FieldSet(
                        _Contains: _shapeFactory.Radio(
                            Id: "contains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that has"), Value: "true", Checked: true),
                        _NotContains: _shapeFactory.Radio(
                            Id: "notContains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that doesn't have"), Value: "false")),
                    _Operator: _shapeFactory.FieldSet(
                        _AnyTerm: _shapeFactory.Radio(
                            Id: "operatorAnyTerm", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Operator),
                            Title: T("Any Term"), Value: "0", Checked: true),
                        _AllTerms: _shapeFactory.Radio(
                            Id: "operatorAllTerms", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Operator),
                            Title: T("All Terms"), Value: "1")),
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
}