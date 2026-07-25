using Autodesk.Revit.DB;

namespace IfcColour
{
    public class MaterialColorItem
    {
        public string MaterialName { get; set; }
        public Color MaterialColor { get; set; }

        public MaterialColorItem()
        {
        }

        public MaterialColorItem(string materialName, Color materialColor)
        {
            MaterialName = materialName;
            MaterialColor = materialColor;
        }

        public MaterialColorItem Clone()
        {
            return new MaterialColorItem(MaterialName ?? "", new Color(MaterialColor.Red, MaterialColor.Green, MaterialColor.Blue));
        }
    }
}
