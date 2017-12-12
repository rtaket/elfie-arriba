using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Commands.Functions;
using XForm.Data;

namespace XForm.Commands.Transformers
{
    public class TrimToUpperTransformer : IColumnValueTransformer
    {
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
                    _transformedArray[i] = ConfluxFunctions.TrimToUpper(originalValue);
                }
            }

            return DataBatch.All(_transformedArray, batch.Count);
        }
    }
}
