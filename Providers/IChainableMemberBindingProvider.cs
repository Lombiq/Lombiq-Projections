using Orchard;
using Orchard.Localization;
using System;
using System.Collections.Generic;

namespace Lombiq.Projections.Providers
{
    public interface IChainableMemberBindingProvider : IDependency
    {
        IEnumerable<ChainableMemberBinding> GetChainableMemberBindings();
    }


    public class ChainableMemberBinding
    {
        public virtual Type ContentPartRecordType { get; set; }
        public virtual string PropertyPath { get; set; }
        public virtual LocalizedString DisplayName { get; set; }
        public virtual LocalizedString Description { get; set; }
    }


    public class ChainableMemberBindingProviderEqualityComparer : IEqualityComparer<ChainableMemberBinding>
    {
        public bool Equals(ChainableMemberBinding x, ChainableMemberBinding y) =>
            x.ContentPartRecordType.FullName == y.ContentPartRecordType.FullName &&
            x.PropertyPath == y.PropertyPath;

        public int GetHashCode(ChainableMemberBinding obj) =>
            $"{obj.ContentPartRecordType.FullName}.{obj.PropertyPath}".GetHashCode();
    }
}
