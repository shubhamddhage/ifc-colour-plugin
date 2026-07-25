using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IfcColour
{
    [Transaction(TransactionMode.Manual)]
    public class UpdateIfcColourCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                var storedRuleSets = RuleSetStorage.LoadOrCreateDefaults();
                var ifcItems = RuleSetStorage.ToMaterialItems(storedRuleSets.IfcExportColours);
                var originalItems = RuleSetStorage.ToMaterialItems(storedRuleSets.OriginalColours);

                using (var form = new MaterialEditorForm(ifcItems, originalItems))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                        return Result.Cancelled;

                    var editedItems = form.GetItems();
                    string activeModeName = form.GetSelectedModeName();

                    RuleSetStorage.Save(new StoredRuleSets
                    {
                        IfcExportColours = RuleSetStorage.ToStoredRules(form.GetIfcItems()),
                        OriginalColours = RuleSetStorage.ToStoredRules(form.GetOriginalItems())
                    });

                    var allMaterials = new FilteredElementCollector(doc)
                        .OfClass(typeof(Material))
                        .Cast<Material>()
                        .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name))
                        .ToList();

                    var materialsByName = allMaterials
                        .GroupBy(m => NormalizeName(m.Name))
                        .ToDictionary(g => g.Key, g => g.First());

                    var updated = new List<string>();
                    var missing = new List<string>();
                    var suggestions = new Dictionary<string, List<string>>();

                    using (Transaction tx = new Transaction(doc, "IFC Colour - Apply Material Rules"))
                    {
                        tx.Start();
                        foreach (var item in editedItems)
                        {
                            if (item == null || string.IsNullOrWhiteSpace(item.MaterialName)) continue;
                            string normalizedTarget = NormalizeName(item.MaterialName);
                            if (string.IsNullOrWhiteSpace(normalizedTarget)) continue;

                            if (materialsByName.TryGetValue(normalizedTarget, out Material material) && material != null)
                            {
                                material.Color = new Color(item.MaterialColor.Red, item.MaterialColor.Green, item.MaterialColor.Blue);
                                updated.Add($"{item.MaterialName} -> RGB({item.MaterialColor.Red}, {item.MaterialColor.Green}, {item.MaterialColor.Blue})");
                            }
                            else
                            {
                                missing.Add(item.MaterialName);
                                var candidateNames = allMaterials.Select(m => m.Name)
                                    .Where(n => !string.IsNullOrWhiteSpace(n) && (NormalizeName(n).Contains(normalizedTarget) || normalizedTarget.Contains(NormalizeName(n))))
                                    .Distinct()
                                    .Take(5)
                                    .ToList();
                                if (candidateNames.Any()) suggestions[item.MaterialName] = candidateNames;
                            }
                        }
                        tx.Commit();
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"Mode: {activeModeName}");
                    sb.AppendLine($"Updated {updated.Count} material(s).");
                    if (updated.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Updated materials:");
                        foreach (var item in updated) sb.AppendLine("- " + item);
                    }
                    if (missing.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"Missing {missing.Count} material(s):");
                        foreach (var name in missing)
                        {
                            sb.AppendLine("- " + name);
                            if (suggestions.ContainsKey(name))
                            {
                                foreach (var candidate in suggestions[name]) sb.AppendLine("    possible match: " + candidate);
                            }
                        }
                    }

                    TaskDialog.Show("IFC Colour", sb.ToString());
                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("IFC Colour Error", ex.ToString());
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name.Trim().Replace('–', '-').Replace('—', '-').Replace("  ", " ").ToUpperInvariant();
        }
    }
}
