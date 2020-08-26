using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedValueListFilterFormElements
    {
        public bool Matches { get; }
        public string StringOperatorString { get; }
        public StringOperator StringOperator { get; }
        public string FilterRelationshipString { get; }
        public ValueFilterRelationship FilterRelationship { get; }
        public string ValueString { get; }
        public string[] Values { get; }


        public TokenizedValueListFilterFormElements(dynamic formState)
        {
            Matches = formState[nameof(Matches)] ?? true;
            StringOperatorString = formState[nameof(StringOperatorString)];
            StringOperator = Enum.TryParse(StringOperatorString, out StringOperator stringOperator) ?
                stringOperator : StringOperator.Equals;
            FilterRelationshipString = formState[nameof(FilterRelationshipString)];
            FilterRelationship = Enum.TryParse(FilterRelationshipString, out ValueFilterRelationship filterRelationship) ?
                filterRelationship : ValueFilterRelationship.Or;
            ValueString = formState[nameof(ValueString)];
            Values = string.IsNullOrEmpty(ValueString) ?
                new string[] { } :
                ValueString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        }
    }

    public static class TokenizedValueListFilterFormElementsExtensions
    {
        public static string[] GetValuesFromJsonString(
            this TokenizedValueListFilterFormElements elements,
            IJsonConverter jsonConverter = null) =>
            string.IsNullOrEmpty(elements.ValueString) ?
                new string[] { } :
                // If the value string is not a JSON array, then it's probably a single value or a comma-separated list.
                elements.ValueString.StartsWith("[") && elements.ValueString.EndsWith("]") &&
                    jsonConverter != null && jsonConverter.TryDeserialize<string[]>(elements.ValueString, out var values) ?
                        values.Distinct().ToArray() : elements.Values;

        public static void GetStringOperatorFilterExpression(
            this TokenizedValueListFilterFormElements elements, IHqlExpressionFactory expression, string property, string value = "")
        {
            switch (elements.StringOperator)
            {
                case StringOperator.ContainedIn:
                    if (elements.Matches) expression.Like(property, Convert.ToString(value), HqlMatchMode.Anywhere);
                    else expression.Not(inner => inner.Like(property, Convert.ToString(value), HqlMatchMode.Anywhere));

                    break;
                case StringOperator.Equals:
                default:
                    if (elements.Matches) expression.Eq(property, value);
                    else expression.Not(inner => inner.Eq(property, value));

                    break;
            }
        }
    }

    public enum ValueFilterRelationship
    {
        Or,
        And
    }

    public enum StringOperator
    {
        Equals,
        ContainedIn
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
                    _MatchOrNoMatch: _shapeFactory.FieldSet(
                        _Match: _shapeFactory.Radio(
                            Id: "match", Name: nameof(TokenizedValueListFilterFormElements.Matches),
                            Title: T("Value(s) match(es)"), Value: "true", Checked: true),
                        _NoMatch: _shapeFactory.Radio(
                            Id: "noMatch", Name: nameof(TokenizedValueListFilterFormElements.Matches),
                            Title: T("Value(s) do(es)n't match"), Value: "false")),
                    _StringOperator: _shapeFactory.FieldSet(
                        _Equals: _shapeFactory.Radio(
                            Id: "equals", Name: nameof(TokenizedValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) equal(s)"), Value: StringOperator.Equals, Checked: true),
                        _ContainedIn: _shapeFactory.Radio(
                            Id: "containedIn", Name: nameof(TokenizedValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) is/are contained in"), Value: StringOperator.ContainedIn)),
                    _Value: _shapeFactory.Textbox(
                        Id: "valueString", Name: nameof(TokenizedValueListFilterFormElements.ValueString),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Value(s)"),
                        Description: T("The optionally tokenized comma-separated list of values.")),
                    _Relationship: _shapeFactory.Textbox(
                        Id: "filterRelationshipString", Name: nameof(TokenizedValueListFilterFormElements.FilterRelationshipString),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Filter relationship"),
                        Description: T("Defines the operator between the filters of individual values. Accepted values: {0}. Default value: \"{1}\".",
                            string.Join(", ", Enum.GetNames(typeof(ValueFilterRelationship))),
                            ValueFilterRelationship.Or.ToString()))));
    }
}