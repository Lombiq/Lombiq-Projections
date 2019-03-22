using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedValueListFilterFormElements
    {
        public bool EqualsOrContainedIn { get; }
        public string FilterRelationshipString { get; }
        public ValueFilterRelationship FilterRelationship { get; }
        public string ValueString { get; }
        public object[] Values { get; }


        public TokenizedValueListFilterFormElements(dynamic formState)
        {
            EqualsOrContainedIn = formState[nameof(EqualsOrContainedIn)] ?? true;
            FilterRelationshipString = formState[nameof(FilterRelationshipString)];
            FilterRelationship = Enum.TryParse(FilterRelationshipString, out ValueFilterRelationship filterRelationship) ?
                filterRelationship : ValueFilterRelationship.Or;
            ValueString = formState[nameof(ValueString)];
            Values = string.IsNullOrEmpty(ValueString) ?
                new object[] { } :
                ValueString
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => (object)value.ToString().Trim())
                    .Where(value => value.ToString() != "")
                    .ToArray();
        }
    }

    public enum ValueFilterRelationship
    {
        Or,
        And
    }

    public class TokenizedValueListFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }

        public static string FormName = nameof(TokenizedValueListFilterForm);


        public TokenizedValueListFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context) =>
            context.Form(FormName, shape =>
                _shapeFactory.Form(
                    Id: FormName,
                    _EqualsOrContainedIn: _shapeFactory.FieldSet(
                        _EqualsOrContained: _shapeFactory.Radio(
                            Id: "equalsToOrContainedIn", Name: nameof(TokenizedValueListFilterFormElements.EqualsOrContainedIn),
                            Title: T("Value is equal to or contained in"), Value: "true", Checked: true),
                        _NotEqualsOrNotContained: _shapeFactory.Radio(
                            Id: "notEqualsToOrNotContainedIn", Name: nameof(TokenizedValueListFilterFormElements.EqualsOrContainedIn),
                            Title: T("Value is not equal to or not contained in"), Value: "false")),
                    _Value: _shapeFactory.Textbox(
                        Id: "valueString", Name: nameof(TokenizedValueListFilterFormElements.ValueString),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Value(s)"),
                        Description: T("The optionally tokenized comma-separated list of values.")),
                    _Relationship: _shapeFactory.Textbox(
                        Id: "filterRelationshipString", Name: nameof(TokenizedValueListFilterFormElements.FilterRelationshipString),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Filter relatioship"),
                        Description: T("Defines the operator between the filter of individual values. Accepted values: {0}. Default value: \"{1}\".",
                            string.Join(", ", Enum.GetNames(typeof(ValueFilterRelationship))),
                            ValueFilterRelationship.Or.ToString()))));
    }
}