using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;
using XForm.IO;

namespace XForm.Query
{
    class BuildStageFactory
    {
        private IDictionary<string,IQueryActionFactory> Factories { get; }

        public BuildStageFactory(params IQueryActionFactory[] factories)
        {
            Factories = new Dictionary<string, IQueryActionFactory>(StringComparer.OrdinalIgnoreCase);
            
            foreach(var queryFactory in factories)
            {
                Factories.Add(queryFactory.Verb, queryFactory);
            }
        }

        public IQueryActionFactory QueryAction(string verb)
        {
            return Factories[verb];
        }
    }

    interface IQueryActionFactory
    {
        string Verb { get; }
        IDataBatchEnumerator CreateBuildStage(IDataBatchEnumerator source, IList<string> configurationParts);
    }

    class QueryRead : IQueryActionFactory
    {
        public string Verb => "read";

        public IDataBatchEnumerator CreateBuildStage(IDataBatchEnumerator source, IList<string> configurationParts)
        {
            if (source != null) throw new ArgumentException("'read' must be the first stage in a pipeline.");
            if (configurationParts.Count != 2) throw new ArgumentException("Usage: 'read' [filePath]");
            if (configurationParts[1].EndsWith("xform"))
            {
                return new BinaryTableReader(configurationParts[1]);
            }
            else
            {
                return new TabularFileReader(configurationParts[1]);
            }
            throw new NotImplementedException();
        }
    }
}
