using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;

namespace XForm.Commands.Functions
{
    public static class ConfluxFunctions
    {
        static readonly byte SPACEBYTE;
        static readonly byte PERIODBYTE;

        static ConfluxFunctions()
        {
            SPACEBYTE = Convert.ToByte(' ');
            PERIODBYTE = Convert.ToByte('.');
        }

        public static DataBatch NetBiosOrDnsToMachineName(IDataBatchEnumerator source, int[] inputColumnIndexes)
        {
            if (inputColumnIndexes == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (inputColumnIndexes.Length != 2)
            {
                throw new ArgumentOutOfRangeException("The inputs DataBatch[] array must contain two DataBatches. The first DataBatch is the NetBios column values. The second DataBatch is the DNS column values.");
            }

            DataBatch netBiosDataBatch = source.ColumnGetter(inputColumnIndexes[0]).Invoke();
            DataBatch dnsDataBatch = source.ColumnGetter(inputColumnIndexes[1]).Invoke();
            String8[] resultArray = null;
            Allocator.AllocateToSize(ref resultArray, netBiosDataBatch.Count);

            String8[] sourceArray = (String8[])netBiosDataBatch.Array;
            for (int i = 0; i < netBiosDataBatch.Count; ++i)
            {
                String8 netBios = sourceArray[netBiosDataBatch.Index(i)];
                String8 dns = sourceArray[dnsDataBatch.Index(i)];

                String8 cleanNetBios = CleanNetBios(netBios);
                resultArray[i] = !cleanNetBios.IsEmpty() ? cleanNetBios : DnsToCleanNetBios(dns);
            }

            return DataBatch.All(resultArray, netBiosDataBatch.Count);
        }

        static String8 DnsToCleanNetBios(String8 dns)
        {
            if (dns == null)
            {
                return String8.Empty;
            }

            int[] positionArray = new int[String8Set.GetLength(dns, PERIODBYTE)];
            String8Set parts = dns.Split(PERIODBYTE, positionArray);

            return CleanNetBios(parts[0]);
        }

        static String8 CleanNetBios(String8 netBios)
        {
            var ret = TrimToUpper(netBios);
            return ret;
        }

        internal static String8 TrimToUpper(String8 str)
        {
            if (str == null)
            {
                return String8.Empty;
            }

            str.ToUpperInvariant();
            str.TrimEnd(SPACEBYTE);

            return str;
        }
    }
}
