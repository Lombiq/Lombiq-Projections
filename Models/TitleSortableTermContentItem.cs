using Lombiq.Projections.Constants;
using Orchard.Data.Conventions;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;

namespace Lombiq.Projections.Models
{
    /// <summary>
    /// Just like <see cref="TermContentItem"/>, this class represents a relationship
    /// between a Content Item and a Term, but also stores the Title of the Term (redundantly),
    /// so we are able to sort content items based on their selected Taxonomy Terms.
    /// If there are multiple Terms selected, the first one (based on <see cref="TermPart"/>'s own sorting)
    /// will be flagged to be used for the comparison.
    /// </summary>
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TitleSortableTermContentItem
    {
        public virtual int Id { get; set; }
        public virtual string Field { get; set; }
        public virtual bool IsFirst { get; set; }
        public virtual string Title { get; set; }
        public virtual TermPartRecord TermPartRecord { get; set; }

        [CascadeAllDeleteOrphan]
        public virtual TitleSortableTermsPartRecord TitleSortableTermsPartRecord { get; set; }
    }
}