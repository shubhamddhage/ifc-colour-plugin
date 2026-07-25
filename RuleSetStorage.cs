using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using Autodesk.Revit.DB;

namespace IfcColour
{
    public static class RuleSetStorage
    {
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IfcColour");
        private static readonly string FilePath = Path.Combine(FolderPath, "rules.json");

        public static StoredRuleSets LoadOrCreateDefaults()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var serializer = new JavaScriptSerializer();
                    var data = serializer.Deserialize<StoredRuleSets>(json);
                    if (data != null)
                    {
                        return new StoredRuleSets
                        {
                            IfcExportColours = Normalize(data.IfcExportColours),
                            OriginalColours = Normalize(data.OriginalColours)
                        };
                    }
                }
            }
            catch
            {
            }

            var defaults = new StoredRuleSets
            {
                IfcExportColours = IfcColourConfig.GetIfcItems().Select(ToDto).ToList(),
                OriginalColours = IfcColourConfig.GetOriginalItems().Select(ToDto).ToList()
            };

            Save(defaults);
            return new StoredRuleSets
            {
                IfcExportColours = Normalize(defaults.IfcExportColours),
                OriginalColours = Normalize(defaults.OriginalColours)
            };
        }

        public static void Save(StoredRuleSets data)
        {
            if (data == null) return;
            Directory.CreateDirectory(FolderPath);
            var safeData = new StoredRuleSets
            {
                IfcExportColours = Normalize(data.IfcExportColours),
                OriginalColours = Normalize(data.OriginalColours)
            };
            var serializer = new JavaScriptSerializer();
            File.WriteAllText(FilePath, serializer.Serialize(safeData));
        }

        public static IList<MaterialColorItem> ToMaterialItems(List<StoredMaterialRule> rules)
        {
            return Normalize(rules)
                .Select(x => new MaterialColorItem(x.MaterialName, new Color((byte)x.Red, (byte)x.Green, (byte)x.Blue)))
                .Select(x => x.Clone())
                .ToList();
        }

        public static List<StoredMaterialRule> ToStoredRules(IList<MaterialColorItem> items)
        {
            if (items == null) return new List<StoredMaterialRule>();
            return items.Where(x => x != null && !string.IsNullOrWhiteSpace(x.MaterialName))
                .Select(x => new StoredMaterialRule
                {
                    MaterialName = x.MaterialName.Trim(),
                    Red = x.MaterialColor.Red,
                    Green = x.MaterialColor.Green,
                    Blue = x.MaterialColor.Blue
                })
                .ToList();
        }

        private static StoredMaterialRule ToDto(MaterialColorItem item)
        {
            return new StoredMaterialRule
            {
                MaterialName = item?.MaterialName ?? "",
                Red = item == null ? 0 : item.MaterialColor.Red,
                Green = item == null ? 0 : item.MaterialColor.Green,
                Blue = item == null ? 0 : item.MaterialColor.Blue
            };
        }

        private static List<StoredMaterialRule> Normalize(List<StoredMaterialRule> rules)
        {
            if (rules == null) return new List<StoredMaterialRule>();
            return rules.Where(x => x != null && !string.IsNullOrWhiteSpace(x.MaterialName))
                .Select(x => new StoredMaterialRule
                {
                    MaterialName = x.MaterialName.Trim(),
                    Red = Clamp(x.Red),
                    Green = Clamp(x.Green),
                    Blue = Clamp(x.Blue)
                })
                .ToList();
        }

        private static int Clamp(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return value;
        }
    }

    public class StoredRuleSets
    {
        public List<StoredMaterialRule> IfcExportColours { get; set; }
        public List<StoredMaterialRule> OriginalColours { get; set; }
    }

    public class StoredMaterialRule
    {
        public string MaterialName { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }
}
