using Lombiq.Projections.Constants;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Lombiq.Projections.Projections.Forms
{
    internal class TokenizedTaxonomyTermsFilterFormElements
    {
        public int Operator { get; set; }
        public string TermProperty { get; set; }
        public string Terms { get; set; }
        public IEnumerable<string> TaxonomyAliases { get; set; }
        public bool Contains { get; set; }


        public TokenizedTaxonomyTermsFilterFormElements(dynamic formState)
        {
            Operator = string.IsNullOrEmpty(formState[nameof(Operator)]?.Value) ? 0 : int.Parse(formState[nameof(Operator)].Value);
            TermProperty = formState[nameof(TermProperty)];
            Terms = formState[nameof(Terms)];
            TaxonomyAliases = string.IsNullOrEmpty(formState[nameof(TaxonomyAliases)]?.Value) ?
                Enumerable.Empty<string>() :
                (formState[nameof(TaxonomyAliases)].Value as string)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();
            Contains = formState[nameof(Contains)] ?? false;
        }
    }

    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TokenizedTaxonomyTermsFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;
        private readonly ITaxonomyService _taxonomyService;

        public Localizer T { get; set; }

        public static string FormName = "MatchingTaxonomyTerms";


        public TokenizedTaxonomyTermsFilterForm(IShapeFactory shapeFactory, ITaxonomyService taxonomyService)
        {
            _shapeFactory = shapeFactory;
            _taxonomyService = taxonomyService;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context)
        {
            Func<IShapeFactory, object> filterForm = shape =>
            {
                var taxonomies = _taxonomyService.GetTaxonomies().OrderBy(taxonomy => taxonomy.Name).ToList();

                var form = _shapeFactory.Form(
                    Id: FormName,
                    _ContainsOrNot: _shapeFactory.FieldSet(
                        _Contains: _shapeFactory.Radio(
                            Id: "contains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that has"), Value: "true", Checked: true
                        ),
                        _NotContains: _shapeFactory.Radio(
                            Id: "notContains", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Contains),
                            Title: T("Content that doesn't have"), Value: "false"
                        )
                    ),
                    _Operator: _shapeFactory.FieldSet(
                        _AnyTerm: _shapeFactory.Radio(
                            Id: "operatorAnyTerm", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Operator),
                            Title: T("Any Term"), Value: "0", Checked: true
                        ),
                        _AllTerms: _shapeFactory.Radio(
                            Id: "operatorAllTerms", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Operator),
                            Title: T("All Terms"), Value: "1"
                        )
                    ),
                    _Property: _shapeFactory.FieldSet(
                        _PropertyId: _shapeFactory.Radio(
                            Id: "termPropertyId", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.TermProperty),
                            Title: T("Defined by the Id"), Value: nameof(TermPart.Id), Checked: true
                        ),
                        _PropertyName: _shapeFactory.Radio(
                            Id: "termPropertyName", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.TermProperty),
                            Title: T("Defined by the Name"), Value: nameof(TermPart.Name)
                        )
                    ),
                    _Terms: _shapeFactory.Textbox(
                        Id: "terms", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.Terms),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Terms"),
                        Description: T("The comma-separated list of Taxonomy Terms (based on their property selected above) to filter content items with.")),
                    _Taxonomies: _shapeFactory.SelectList(
                        Id: "taxonomyId", Name: nameof(TokenizedTaxonomyTermsFilterFormElements.TaxonomyAliases),
                        Title: T("That belong to the Taxonomy"),
                        Description: T("Select the Taxonomy that the Terms to filter on belong to. When filtering Terms based on their Id, selecting Taxonomies does not have an effect, but filtering on their Names may yield unexpected results when a Taxonomy is not defined and the same Term name occurs across multiple Taxonomies on the given content."),
                        Size: Math.Min(taxonomies.Count() + 1, 11),
                        Multiple: true)
                );

                form._Taxonomies.Add(new SelectListItem { Value = "", Text = T("<None>").Text });
                foreach (var taxonomy in taxonomies)
                    form._Taxonomies.Add(new SelectListItem
                    {
                        Value = taxonomy.ContentItem?.As<IAliasAspect>().Path ?? taxonomy.Id.ToString(),
                        Text = taxonomy.Name
                    });

                return form;
            };

            context.Form(FormName, filterForm);
        }
    }
}