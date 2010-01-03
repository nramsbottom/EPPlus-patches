﻿/* 
 * You may amend and distribute as you like, but don't remove this header!
 * 
 * EPPlus provides server-side generation of Excel 2007 spreadsheets.
 * EPPlus is a fork of the ExcelPackage project
 * See http://www.codeplex.com/EPPlus for details.
 * 
 * All rights reserved.
 * 
 * EPPlus is an Open Source project provided under the 
 * GNU General Public License (GPL) as published by the 
 * Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 * 
 * The GNU General Public License can be viewed at http://www.opensource.org/licenses/gpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 * 
 * The code for this project may be used and redistributed by any means PROVIDING it is 
 * not sold for profit without the author's written consent, and providing that this notice 
 * and the author's name and all copyright notices remain intact.
 * 
 * All code and executables are provided "as is" with no warranty either express or implied. 
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * 
 * Code change notes:
 * 
 * Author							Change						Date
 * ******************************************************************************
 * Jan Källman		                Initial Release		        2009-10-01
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Packaging;
using System.Xml;
using System.Collections;
using System.IO;
using System.Drawing;
using OfficeOpenXml.Drawing.Chart;
namespace OfficeOpenXml.Drawing
{
    /// <summary>
    /// Enables access to the Drawings.
    /// VERY basic support for Charts, Shapes and Pictures in present version.
    /// </summary>
    public class ExcelDrawings : IEnumerable
    {
        private XmlDocument _drawingsXml=new XmlDocument();
        private Dictionary<string, int> _drawingNames;
        private List<ExcelDrawing> _drawings;
        public ExcelDrawings(ExcelPackage xlPackage, ExcelWorksheet sheet)
        {
                _drawingsXml = new XmlDocument();
                _drawingsXml.PreserveWhitespace = true;
                _drawings = new List<ExcelDrawing>();
                _drawingNames = new Dictionary<string,int>();
                Worksheet = sheet;
                XmlNode node = sheet.WorksheetXml.SelectSingleNode("//d:drawing", sheet.NameSpaceManager);
                CreateNSM();
                if (node != null)
                {
                    PackageRelationship drawingRelation = sheet.Part.GetRelationship(node.Attributes["r:id"].Value);
                    _uriDrawing = PackUriHelper.ResolvePartUri(sheet.WorksheetUri, drawingRelation.TargetUri);

                    _part = xlPackage.Package.GetPart(_uriDrawing);
                    _drawingsXml.Load(_part.GetStream());

                    AddDrawings();
                }
         }
        internal ExcelWorksheet Worksheet { get; set; }
        public XmlDocument DrawingXml
        {
            get
            {
                return _drawingsXml;
            }
        }
        private void AddDrawings()
        {
            XmlNodeList list = _drawingsXml.SelectNodes("//xdr:twoCellAnchor", NameSpaceManager);

            int i = 1;
            foreach (XmlNode node in list)
            {
                ExcelDrawing dr = ExcelDrawing.GetDrawing(this, node);
                _drawings.Add(dr);
                if (!_drawingNames.ContainsKey(dr.Name))
                {
                    _drawingNames.Add(dr.Name, _drawings.Count - 1);
                }
            }
        }


        #region NamespaceManager
        /// <summary>
        /// Creates the NamespaceManager. 
        /// </summary>
        private void CreateNSM()
        {
            NameTable nt = new NameTable();
            _nsManager = new XmlNamespaceManager(nt);
            _nsManager.AddNamespace("a", ExcelPackage.schemaDrawings);
            _nsManager.AddNamespace("xdr", ExcelPackage.schemaSheetDrawings);
            _nsManager.AddNamespace("c", ExcelPackage.schemaChart);
            _nsManager.AddNamespace("r", ExcelPackage.schemaRelationships);
        }
        /// <summary>
        /// Provides access to a namespace manager instance to allow XPath searching
        /// </summary>
        XmlNamespaceManager _nsManager=null;
        public XmlNamespaceManager NameSpaceManager
        {
            get
            {
                return _nsManager;
            }
        }
        #endregion
        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return (_drawings.GetEnumerator());
        }
        /// <summary>
        /// Returns the worksheet at the specified position.  
        /// </summary>
        /// <param name="PositionID">The position of the worksheet. 1-base</param>
        /// <returns></returns>
        public ExcelDrawing this[int PositionID]
        {
            get
            {
                return (_drawings[PositionID]);
            }
        }

        /// <summary>
        /// Returns the drawing matching the specified name
        /// </summary>
        /// <param name="Name">The name of the worksheet</param>
        /// <returns></returns>
        public ExcelDrawing this[string Name]
        {
            get
            {
                if (_drawingNames.ContainsKey(Name))
                {
                    return _drawings[_drawingNames[Name]];
                }
                else
                {
                    return null;
                }
            }
        }
        public int Count
        {
            get
            {
                if (_drawings == null)
                {
                    return 0;
                }
                else
                {
                    return _drawings.Count;
                }
            }
        }
        PackagePart _part=null;
        internal PackagePart Part
        {
            get
            {
                return _part;
            }        
        }
        Uri _uriDrawing=null;
        public Uri UriDrawing
        {
            get
            {
                return _uriDrawing;
            }
        }
        #endregion
        #region "Add functions"
            /// <summary>
            /// Adds a new shart to the worksheet
            /// </summary>
            /// <param name="Name"></param>
            /// <param name="ChartType">Type of chart</param>
            /// <returns></returns>
            public ExcelChart AddChart(string Name, eChartType ChartType)
            {
                if(_drawingNames.ContainsKey(Name))
                {
                    throw new Exception("Name already exist in the drawings collection");
                }

                if (ChartType == eChartType.Bubble ||
                    ChartType == eChartType.Bubble3DEffect ||
                    ChartType == eChartType.Radar ||
                    ChartType == eChartType.RadarFilled ||
                    ChartType == eChartType.RadarMarkers ||
                    ChartType == eChartType.StockHLC ||
                    ChartType == eChartType.StockOHLC ||
                    ChartType == eChartType.StockVOHLC ||
                    ChartType == eChartType.Surface ||
                    ChartType == eChartType.SurfaceTopView ||
                    ChartType == eChartType.SurfaceTopViewWireframe ||
                    ChartType == eChartType.SurfaceWireframe)
                {
                    throw(new NotImplementedException("Chart type not supported in this version"));
                }

                XmlElement drawNode = CreateDrawingXml();

                
                ExcelChart chart = GetNewChart(drawNode, ChartType);
                chart.Name = Name;
                _drawings.Add(chart);
                _drawingNames.Add(Name, _drawings.Count - 1);
                return chart;
            }
            /// <summary>
            /// Adds a picure to the worksheet
            /// </summary>
            /// <param name="Name"></param>
            /// <param name="image">An image. Allways saved in then JPeg format</param>
            /// <returns></returns>
            public ExcelPicture AddPicture(string Name, Image image)
            {
                if (image != null)
                {
                    if (_drawingNames.ContainsKey(Name))
                    {
                        throw new Exception("Name already exist in the drawings collection");
                    }                    
                    XmlElement drawNode = CreateDrawingXml();
                    drawNode.SetAttribute("editAs", "oneCell");
                    ExcelPicture pic = new ExcelPicture(this, drawNode, image);
                    pic.Name = Name;
                    _drawings.Add(pic);
                    _drawingNames.Add(Name, _drawings.Count - 1);
                    return pic;
                }
                throw (new Exception("AddPicture: Image can't be null"));
            }
            public ExcelShape AddShape(string Name, eShapeStyle Style)
            {
                if (_drawingNames.ContainsKey(Name))
                {
                    throw new Exception("Name already exist in the drawings collection");
                }
                XmlElement drawNode = CreateDrawingXml();
                ExcelShape shape = new ExcelShape(this, drawNode, Style);
                shape.Name = Name;
                shape.Style = Style;
                _drawings.Add(shape);
                _drawingNames.Add(Name, _drawings.Count - 1);
                return shape;
            }
            private ExcelChart GetNewChart(XmlNode drawNode, eChartType chartType)
            {
                switch(chartType)
                {
                    case eChartType.Pie:
                    case eChartType.PieExploded:
                    case eChartType.Pie3D:
                    case eChartType.PieExploded3D:
                        return new ExcelPieChart(this, drawNode, chartType);
                    case eChartType.BarOfPie:
                    case eChartType.PieOfPie:
                        return new ExcelOfPieChart(this, drawNode, chartType);
                    case eChartType.Doughnut:
                    case eChartType.DoughnutExploded:
                        return new ExcelDoughnutChart(this, drawNode, chartType);
                    case eChartType.BarClustered:
                    case eChartType.BarStacked:
                    case eChartType.BarStacked100:
                    case eChartType.BarClustered3D:
                    case eChartType.BarStacked3D:
                    case eChartType.BarStacked1003D:
                    case eChartType.ConeBarClustered:
                    case eChartType.ConeBarStacked:
                    case eChartType.ConeBarStacked100:
                    case eChartType.CylinderBarClustered:
                    case eChartType.CylinderBarStacked:
                    case eChartType.CylinderBarStacked100:
                    case eChartType.PyramidBarClustered:
                    case eChartType.PyramidBarStacked:
                    case eChartType.PyramidBarStacked100:
                    case eChartType.ColumnClustered:
                    case eChartType.ColumnStacked:
                    case eChartType.ColumnStacked100:
                    case eChartType.Column3D:
                    case eChartType.ColumnClustered3D:
                    case eChartType.ColumnStacked3D:
                    case eChartType.ColumnStacked1003D:
                    case eChartType.ConeCol:
                    case eChartType.ConeColClustered:
                    case eChartType.ConeColStacked:
                    case eChartType.ConeColStacked100:
                    case eChartType.CylinderCol:
                    case eChartType.CylinderColClustered:
                    case eChartType.CylinderColStacked:
                    case eChartType.CylinderColStacked100:
                    case eChartType.PyramidCol:
                    case eChartType.PyramidColClustered:
                    case eChartType.PyramidColStacked:
                    case eChartType.PyramidColStacked100:
                        return new ExcelBarChart(this, drawNode, chartType);
                    case eChartType.XYScatter:
                    case eChartType.XYScatterLines:
                    case eChartType.XYScatterLinesNoMarkers:
                    case eChartType.XYScatterSmooth:
                    case eChartType.XYScatterSmoothNoMarkers:
                        return new ExcelScatterChart(this, drawNode, chartType);
                    default:
                        return new ExcelChart(this, drawNode, chartType);
                }
            }
            private XmlElement CreateDrawingXml()
            {
                if (DrawingXml.OuterXml == "")
                {
                    DrawingXml.LoadXml(string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><xdr:wsDr xmlns:xdr=\"{0}\" xmlns:a=\"{1}\" />", ExcelPackage.schemaSheetDrawings, ExcelPackage.schemaDrawings));
                    _uriDrawing = new Uri(string.Format("/xl/drawings/drawing{0}.xml", Worksheet.SheetID),UriKind.Relative);

                    Package package = Worksheet.xlPackage.Package;
                    _part = package.CreatePart(_uriDrawing, "application/vnd.openxmlformats-officedocument.drawing+xml", CompressionOption.Maximum);

                    StreamWriter streamChart = new StreamWriter(_part.GetStream(FileMode.Create, FileAccess.Write));
                    DrawingXml.Save(streamChart);
                    streamChart.Close();
                    package.Flush();

                    PackageRelationship drawRelation = Worksheet.Part.CreateRelationship(_uriDrawing, TargetMode.Internal, ExcelPackage.schemaRelationships + "/drawing");
                    XmlElement e = Worksheet.WorksheetXml.CreateElement("drawing", ExcelPackage.schemaMain);
                    e.SetAttribute("id",ExcelPackage.schemaRelationships, drawRelation.Id);

                    Worksheet.WorksheetXml.DocumentElement.AppendChild(e);
                    package.Flush();                    
                }
                XmlNode colNode = _drawingsXml.SelectSingleNode("//xdr:wsDr", NameSpaceManager);
                XmlElement drawNode = _drawingsXml.CreateElement("xdr", "twoCellAnchor", ExcelPackage.schemaSheetDrawings);
                colNode.AppendChild(drawNode);

                //Add from position Element;
                XmlElement fromNode = _drawingsXml.CreateElement("xdr","from", ExcelPackage.schemaSheetDrawings);
                drawNode.AppendChild(fromNode);
                fromNode.InnerXml = "<xdr:col>0</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>0</xdr:row><xdr:rowOff>0</xdr:rowOff>";

                //Add to position Element;
                XmlElement toNode = _drawingsXml.CreateElement("xdr", "to", ExcelPackage.schemaSheetDrawings);
                drawNode.AppendChild(toNode);
                toNode.InnerXml = "<xdr:col>10</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>10</xdr:row><xdr:rowOff>0</xdr:rowOff>";
                return drawNode;
            }
        #endregion
    }
}