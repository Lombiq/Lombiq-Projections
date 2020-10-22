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

    public abstract class TokenizedValueListFilterFormBase : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }


        public TokenizedValueListFilterFormBase(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        // This won't do anything, just here for demonstation.
        public virtual void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedValueListFilterFormBase), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedValueListFilterFormBase),
                    _MatchOrNoMatch: GetMatchRadioFieldSet(),
                    _Value: GetValueTextbox(),
                    _Relationship: GetFilterRelationshipTextbox()));


        public virtual dynamic GetMatchRadioFieldSet() => _shapeFactory.FieldSet(
            _Match: _shapeFactory.Radio(
                Id: "match", Name: nameof(TokenizedValueListFilterFormElements.Matches),
                Title: T("Value(s) match(es)"), Value: "true", Checked: true),
            _NoMatch: _shapeFactory.Radio(
                Id: "noMatch", Name: nameof(TokenizedValueListFilterFormElements.Matches),
                Title: T("Value(s) do(es)n't match"), Value: "false"));

        public virtual dynamic GetValueTextbox() => _shapeFactory.Textbox(
            Id: "valueString", Name: nameof(TokenizedValueListFilterFormElements.ValueString),
            Classes: new[] { "text", "medium", "tokenized" },
            Title: T("Value(s)"),
            Description: T("The optionally tokenized comma-separated list of values."));

        public virtual dynamic GetFilterRelationshipTextbox() => _shapeFactory.Textbox(
            Id: "filterRelationshipString", Name: nameof(TokenizedValueListFilterFormElements.FilterRelationshipString),
            Classes: new[] { "text", "medium", "tokenized" },
            Title: T("Filter relationship"),
            Description: T("Defines the operator between the filters of individual values. Accepted values: {0}. Default value: \"{1}\".",
                string.Join(", ", Enum.GetNames(typeof(ValueFilterRelationship))),
                ValueFilterRelationship.Or.ToString()));
    }


    public enum ValueFilterRelationship
    {
        Or,
        And
    }
}