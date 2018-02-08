using Lombiq.Projections.Constants;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;

namespace Lombiq.Projections.Projections.Forms
{
    [OrchardFeature(FeatureNames.Fields)]
    public class TokenizedBooleanFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;
        public static string FormName = nameof(TokenizedBooleanFilterForm);

        public Localizer T { get; set; }


        public TokenizedBooleanFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context)
        {
            object filterForm(IShapeFactory shape)
            {
                var form = _shapeFactory.Form(
                    Id: FormName,
                    _Terms: _shapeFactory.Textbox(
                        Id: "value", Name: "Value",
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Tokenized value"),
                        Description: T("The tokenized value of the BooleanField.")
                    )
                );

                return form;
            }

            context.Form(FormName, filterForm);
        }
    }
}