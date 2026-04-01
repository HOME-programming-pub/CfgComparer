using CfgCompLib.classes;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace CfgCompLib {
    public static class CfgFromFlowChart {
        public static Graph GenerateGraphFromXML(string xmlPath)
        {
            Graph graph = new();

            string xsdPath = Configuration.config.GetRequiredSection("Settings").GetValue<string>("XSDPath");
            if (String.IsNullOrEmpty(xsdPath)) {
                throw new FileNotFoundException("*.xsd fíle for validating flow chart xml not found");
            } 

            XmlReaderSettings settings = new(); 
            settings.Schemas.Add("", xsdPath);  //Load XSD into settings without set namespace
            settings.ValidationType = ValidationType.Schema;

            XmlReader reader = XmlReader.Create(xmlPath, settings);
            
            XmlDocument document = new();
            
            try {          
                document.Load(reader);  //Load XML document and validate with XSD from settings
            } catch (Exception ex) {
                throw new XmlSchemaValidationException("XML validation failed: " + ex.Message);
            }
            XmlNode root = document.DocumentElement;
            XmlNodeList vertices = root.SelectNodes("//*[@vertex='1']");  //go from root and get nodes,        
            XmlNodeList edges = root.SelectNodes("//*[@edge='1']");       //edges by set attribute = 1

            Dictionary<string,int> idMapping = [];
            int graphId = 0;

            foreach (XmlElement vertex in vertices)
            {
                idMapping.Add(vertex.GetAttribute("id"), graphId);  //provide new IDs for nodes

                // For some reason, a shape Note can have multiple child Nodes and we do not know which one is the mxGeometry Node
                // Therefore, we need to iterate through all child nodes and check for the name "mxGeometry" to extract the shape properties
                XmlNode attributeNode = null;
                foreach (XmlNode attributeNodes in vertex.ChildNodes)
                {
                    if(attributeNodes.Name.Equals("mxGeometry")) { attributeNode = attributeNodes; break; }
                }
                if (attributeNode == null) { throw new Exception("Incorrect format for a draw.io XML-schema"); }

                // Extracts the Node Position and Size properties
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                foreach (XmlNode attribute in attributeNode.Attributes)
                {
                    switch (attribute.Name)
                    {
                        case "x": x = int.Parse(attribute.Value); break;
                        case "y": y = int.Parse(attribute.Value); break;
                        case "width": width = int.Parse(attribute.Value); break;
                        case "height": height = int.Parse(attribute.Value); break;

                    }
                }

                graph.AddNode(new Node(graphId, PrepareLabel(vertex.GetAttribute("value")), null, null, new ShapeProperties(x, y, width, height, Shape.Action))); //get label content
                graphId++;
            }
            ;

            foreach (XmlElement edge in edges) {
                try { 

                int sourceId = idMapping[edge.GetAttribute("source")]; //create graph edges
                int targetId = idMapping[edge.GetAttribute("target")]; //by attributes "source" + "target"      
                graph.AddEdge(graph.GetNode(sourceId), graph.GetNode(targetId));

                } catch (KeyNotFoundException ex){   
                    throw new KeyNotFoundException($"Edge could not be added to the flow chart graph, because source or target with ID {ex.Message.Split("'")[1]} not vertex in the graph");
                }; 
            };

            return graph;
        }
        public static List<string> PrepareLabel(string source) {
            
            //Drawio flow charts may contain HTML elements in "value" for line breaks, style... and HTML encoding - this needs to be removed
            //also Multiline flow elements need to be separated into expression by split at ";"
            string decodedString = WebUtility.HtmlDecode(source);
            decodedString = Regex.Replace(decodedString, "<[/]?div>", ";");
            decodedString = Regex.Replace(decodedString, "<br>", ";");
            List<string> expressions = [.. decodedString.Split(";", StringSplitOptions.RemoveEmptyEntries)];
            
            for (int i=0; i<expressions.Count; i++) {
                expressions[i] = Regex.Replace(expressions[i], "<[^<.]+>", "").Trim();
                if (String.IsNullOrEmpty(expressions[i])) {
                    expressions.RemoveAt(i);
                }
            }
            return expressions;
        }
    }
}
