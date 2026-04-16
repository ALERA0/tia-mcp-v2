using Siemens.Engineering;
using Siemens.Engineering.SW.Blocks;
using System;
using System.Collections.Generic;
using TiaMcpV2.Models;

namespace TiaMcpV2.Helpers
{
    public static class AttributeHelper
    {
        public static List<AttributeInfo> GetAttributes(IEngineeringObject obj)
        {
            var attributes = new List<AttributeInfo>();
            if (obj == null) return attributes;

            foreach (var attr in obj.GetAttributeInfos())
            {
                try
                {
                    object value = obj.GetAttribute(attr.Name);
                    attributes.Add(new AttributeInfo
                    {
                        Name = attr.Name,
                        Value = value,
                        AccessMode = Enum.GetName(typeof(EngineeringAttributeAccessMode), attr.AccessMode)
                    });
                }
                catch
                {
                    attributes.Add(new AttributeInfo
                    {
                        Name = attr.Name,
                        Value = "<unreadable>",
                        AccessMode = Enum.GetName(typeof(EngineeringAttributeAccessMode), attr.AccessMode)
                    });
                }
            }
            return attributes;
        }

        public static BlockGroupInfo BuildBlockHierarchy(PlcBlockGroup group, string parentPath = "")
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? group.Name : $"{parentPath}/{group.Name}";
            var info = new BlockGroupInfo { Name = group.Name, Path = currentPath };

            var blocks = new List<BlockInfo>();
            foreach (var block in group.Blocks)
            {
                blocks.Add(new BlockInfo
                {
                    Name = block.Name,
                    TypeName = block.GetType().Name,
                    Number = block.Number,
                    ProgrammingLanguage = Enum.GetName(typeof(ProgrammingLanguage), block.ProgrammingLanguage),
                    MemoryLayout = Enum.GetName(typeof(MemoryLayout), block.MemoryLayout),
                    IsConsistent = block.IsConsistent,
                    ModifiedDate = block.ModifiedDate,
                    IsKnowHowProtected = block.IsKnowHowProtected,
                    GroupPath = currentPath
                });
            }
            info.Blocks = blocks;

            var groups = new List<BlockGroupInfo>();
            foreach (var sub in group.Groups)
            {
                groups.Add(BuildBlockHierarchy(sub, currentPath));
            }
            info.Groups = groups;

            return info;
        }
    }
}
