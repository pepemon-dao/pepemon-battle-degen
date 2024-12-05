using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;

namespace Contracts.PepemonFactory.abi.ContractDefinition
{
    [FunctionOutput]
    [JsonArray(allowNullItems: true)]
    public class BatchGetBattleCardStatsOutputDTO : IFunctionOutputDTO
    {
        [Parameter("tuple[]", "", 1)]
        public virtual List<BattleCardStats> ReturnValue1 { get; set; } = new();

        public static BatchGetBattleCardStatsOutputDTO FromObject(object[][] items)
        {
            var ret = new BatchGetBattleCardStatsOutputDTO();
            foreach(var i in items)
            {
                ret.ReturnValue1.Add(BattleCardStats.FromObject(i));
            }
            return ret;
        }
    }

    [JsonArray(allowNullItems: true)]
    public class BattleCardStats
    {
        [Parameter("uint16", "element", 1)]
        public virtual ushort Element { get; set; }
        [Parameter("uint16", "hp", 2)]
        public virtual ushort Hp { get; set; }
        [Parameter("uint16", "speed", 3)]
        public virtual ushort Speed { get; set; }
        [Parameter("uint16", "intelligence", 4)]
        public virtual ushort Intelligence { get; set; }
        [Parameter("uint16", "defense", 5)]
        public virtual ushort Defense { get; set; }
        [Parameter("uint16", "attack", 6)]
        public virtual ushort Attack { get; set; }
        [Parameter("uint16", "specialAttack", 7)]
        public virtual ushort SpecialAttack { get; set; }
        [Parameter("uint16", "specialDefense", 8)]
        public virtual ushort SpecialDefense { get; set; }
        [Parameter("uint16", "level", 9)]
        public virtual ushort Level { get; set; }
        [Parameter("string", "name", 10)]
        public virtual string Name { get; set; }
        [Parameter("string", "description", 11)]
        public virtual string Description { get; set; }
        [Parameter("string", "ipfsAddr", 12)]
        public virtual string IpfsAddr { get; set; }
        [Parameter("string", "rarity", 13)]
        public virtual string Rarity { get; set; }

        public static BattleCardStats FromObject(object[] item)
        {
            if (item.Count() != 13) return new BattleCardStats(); // Ensure the list has the expected length
            
            try
            {
                return new BattleCardStats
                {
                    Element = Convert.ToUInt16(item[0]),
                    Hp = Convert.ToUInt16(item[1]),
                    Speed = Convert.ToUInt16(item[2]),
                    Intelligence = Convert.ToUInt16(item[3]),
                    Defense = Convert.ToUInt16(item[4]),
                    Attack = Convert.ToUInt16(item[5]),
                    SpecialAttack = Convert.ToUInt16(item[6]),
                    SpecialDefense = Convert.ToUInt16(item[7]),
                    Level = Convert.ToUInt16(item[8]),
                    Name = item[9].ToString(),
                    Description = item[10].ToString(),
                    IpfsAddr = item[11].ToString(),
                    Rarity = item[12].ToString()
                };
            }
            catch (Exception e)
            {
                Debug.LogError("uanble to convert value for BattleCardStats. " + e.ToString());
                return new BattleCardStats();
            }
        }
    }
}
