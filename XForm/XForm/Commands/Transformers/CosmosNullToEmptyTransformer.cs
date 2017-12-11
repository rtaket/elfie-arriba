using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;

namespace XForm.Commands.Transformers
{
    public class CosmosNullToEmptyTransformer : IColumnValueTransformer
    {
        const string COSMOSNULL = "#NULL#";
        String8[] _transformedArray;

        public DataBatch Transform(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _transformedArray, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 originalValue = sourceArray[batch.Index(i)];

                if (originalValue == null)
                {
                    _transformedArray[i] = originalValue;
                }
                else
                {
                    _transformedArray[i] = originalValue.CompareTo(COSMOSNULL) == 0 ? String8.Empty : originalValue;
                }
            }

            return DataBatch.All(_transformedArray, batch.Count);
        }
    }
}
