using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using UnityEngine;
using System.Linq;

namespace Contracts.PepemonFactory.abi.ContractDefinition
{
    [FunctionOutput]
    public class BatchGetSupportCardStatsOutputDTO : IFunctionOutputDTO
    {
        [Parameter("tuple[]", "", 1)]
        public virtual List<SupportCardStats> ReturnValue1 { get; set; } = new();

        public static BatchGetSupportCardStatsOutputDTO FromObject(object[][] items)
        {
            var ret = new BatchGetSupportCardStatsOutputDTO();
            foreach (var i in items)
            {
                ret.ReturnValue1.Add(SupportCardStats.FromObject(i));
            }
            return ret;
        }
    }

    public class SupportCardStats
    {
        [Parameter("bytes32", "currentRoundChanges", 1)]
        public virtual byte[] CurrentRoundChanges { get; set; }
        [Parameter("bytes32", "nextRoundChanges", 2)]
        public virtual byte[] NextRoundChanges { get; set; }
        [Parameter("uint256", "specialCode", 3)]
        public virtual BigInteger SpecialCode { get; set; }
        [Parameter("uint16", "modifierNumberOfNextTurns", 4)]
        public virtual ushort ModifierNumberOfNextTurns { get; set; }
        [Parameter("bool", "isOffense", 5)]
        public virtual bool IsOffense { get; set; }
        [Parameter("bool", "isNormal", 6)]
        public virtual bool IsNormal { get; set; }
        [Parameter("bool", "isStackable", 7)]
        public virtual bool IsStackable { get; set; }
        [Parameter("string", "name", 8)]
        public virtual string Name { get; set; }
        [Parameter("string", "description", 9)]
        public virtual string Description { get; set; }
        [Parameter("string", "ipfsAddr", 10)]
        public virtual string IpfsAddr { get; set; }
        [Parameter("string", "rarity", 11)]
        public virtual string Rarity { get; set; }

        // expect that the byte32 data looks like this: "0x0000000000000000f44100000000000000000000000000000000000000000000"
        public static byte[] ByteArrayFromStrByte32(object obj)
        {
            try
            {
                string hex = (string)obj;
                if (hex.StartsWith("0x")) hex = hex.Substring(2); // Remove the "0x" prefix if present
                return Enumerable.Range(0, hex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                            .ToArray();
            }
            catch(Exception e)
            {
                Debug.LogError($"unable to convert byte32 array in SupportCardStats. data={obj}, error={e.Message}");
            }
            return new byte[0];
        }

        public static SupportCardStats FromObject(object[] objects)
        {
            if (objects.Length != 11) return new SupportCardStats();

            try
            {
                return new SupportCardStats
                {
                    CurrentRoundChanges = ByteArrayFromStrByte32(objects[0]),
                    NextRoundChanges = ByteArrayFromStrByte32(objects[1]),
                    SpecialCode = Convert.ToUInt64(objects[2]),
                    ModifierNumberOfNextTurns = Convert.ToUInt16(objects[3]),
                    IsOffense = Convert.ToBoolean(objects[4]),
                    IsNormal = Convert.ToBoolean(objects[5]),
                    IsStackable = Convert.ToBoolean(objects[6]),
                    Name = objects[7].ToString(),
                    Description = objects[8].ToString(),
                    IpfsAddr = objects[9].ToString(),
                    Rarity = objects[10].ToString()
                };
            }
            catch (Exception e)
            {
                Debug.LogError("unable to convert value for SupportCardStats. " + e.ToString());
                return new SupportCardStats();
            }
        }
    }
}
