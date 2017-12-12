using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;

namespace XForm.Commands.Transformers
{
    public interface IColumnValueTransformer
    {
        DataBatch Transform(DataBatch batch, string[] arguments);
    }
}