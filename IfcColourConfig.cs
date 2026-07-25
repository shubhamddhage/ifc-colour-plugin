using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace IfcColour
{
    public static class IfcColourConfig
    {
        public static IList<MaterialColorItem> GetIfcItems()
        {
            return new List<MaterialColorItem>
            {
                new MaterialColorItem("CIP - CMU Walls", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - Walls", new Color(255, 0, 255)),
                new MaterialColorItem("CIP - Columns", new Color(240, 240, 0)),
                new MaterialColorItem("CIP - FTGs", new Color(90, 180, 180)),
                new MaterialColorItem("CIP - Beams", new Color(255, 128, 30)),
                new MaterialColorItem("CIP - Slabs", new Color(0, 64, 164))
            };
        }

        public static IList<MaterialColorItem> GetOriginalItems()
        {
            return new List<MaterialColorItem>
            {
                new MaterialColorItem("CIP - CMU Walls", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - Walls", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - Columns", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - FTGs", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - Beams", new Color(128, 128, 128)),
                new MaterialColorItem("CIP - Slabs", new Color(128, 128, 128))
            };
        }
    }
}
