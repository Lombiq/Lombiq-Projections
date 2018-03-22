using System;
using System.Linq;
using System.Reflection;
using Orchard.Data;
using Orchard.Environment.ShellBuilders.Models;
using Orchard.Projections.Models;

namespace Orchard.Projections.Services
{
    public class CustomMemberReferenceBindingProvider : IMemberBindingProvider
    {
        private readonly IRepository<MemberBindingRecord> _repository;
        private readonly ISessionFactoryHolder _sessionFactoryHolder;

        public CustomMemberReferenceBindingProvider(
            IRepository<MemberBindingRecord> repository,
            ISessionFactoryHolder sessionFactoryHolder)
        {
            _repository = repository;
            _sessionFactoryHolder = sessionFactoryHolder;
        }

        public void GetMemberBindings(BindingBuilder builder)
        {
            var recordBluePrints = _sessionFactoryHolder.GetSessionFactoryParameters().RecordDescriptors;

            foreach (var member in _repository.Table.ToList().Where(member => member.Member.Contains('.')))
            {
                var initialRecord = recordBluePrints.FirstOrDefault(r => String.Equals(r.Type.FullName, member.Type, StringComparison.OrdinalIgnoreCase));

                if (initialRecord == null) continue;

                var segments = member.Member.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(segment => segment.Trim());
                RecordBlueprint referencedRecord = initialRecord;
                PropertyInfo property = null;

                foreach (var segment in segments)
                {
                    property = referencedRecord.Type.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
                    if (property == null) break;
                    if (segment != (segments.LastOrDefault() ?? ""))
                        referencedRecord = recordBluePrints
                            .FirstOrDefault(r => String.Equals(r.Type.FullName, property.PropertyType.FullName, StringComparison.OrdinalIgnoreCase));
                }

                if (property != null && property.Name == (segments.LastOrDefault() ?? ""))
                    builder.Add(property, member.DisplayName, member.Description);
            }
        }
    }
}