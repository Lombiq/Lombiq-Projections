using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using System;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedBooleanFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;
        public static string FormName = typeof(TokenizedBooleanFilterForm).Name;

        public Localizer T { get; set; }


        public TokenizedBooleanFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context)
        {
            Func<IShapeFactory, object> filterForm = shape =>
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
            };

            context.Form(FormName, filterForm);
        }
    }
}