using Hover3dModelConvert.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Nodes;
using System.Xml.Serialization;

namespace Hover3dModelConvert // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static Dictionary<int, int> pointIndexes = new Dictionary<int, int>();

        static Dictionary<string, int> pointStringIndexes = new Dictionary<string, int>();


        static void Main(string[] args)
        {

            // BuildFromJson();
            BuildFromXML();
        }

        private static void BuildFromXML()
        {
            FileStream fileStream = File.OpenWrite("hoverModelFromXml.obj");
            StreamWriter streamWriter = streamWriter = new StreamWriter(
                                   fileStream,
                                   encoding: null,
                                   bufferSize: -1,
                                   leaveOpen: true);



            var hoverJson = File.ReadAllText("3dmodel2.xml");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DATA_EXPORT));

            DATA_EXPORT? data;
            // десериализуем объект
            using (FileStream fs = new FileStream("3dmodel2.xml", FileMode.OpenOrCreate))
            {
                data = xmlSerializer.Deserialize(fs) as DATA_EXPORT;
            }
            int i = 1;
            foreach (var point in data.STRUCTURES.ROOF.POINTS)
            {
                pointStringIndexes.Add(point.id, i);

                streamWriter.WriteLine($"v {point.data.Replace(",", " ").Replace(".", ",")}");
                i++;
            }

            foreach (var face in data.STRUCTURES.ROOF.FACES)
            {
                streamWriter.WriteLine($"g {face.type}");

                var lines = face.POLYGON.path.Split(',');

                List<int> facePoints = new List<int>();
                foreach (var line in lines)
                {
                    var points = data.STRUCTURES.ROOF.LINES.FirstOrDefault(x => x.id == line).path.Split(',');

                    facePoints.AddRange(points.Select(x => pointStringIndexes[x]));
                }

                streamWriter.WriteLine($"f {string.Join(' ', facePoints)}");

            }
        }



        private static void BuildFromJson()
        {


            var hoverJson = File.ReadAllText("3dmodel1.json");
            var hoverRoot = JsonNode.Parse(hoverJson).AsObject();
            FileStream fileStream = File.OpenWrite("hoverModel.obj");
            StreamWriter streamWriter = new StreamWriter(
                                   fileStream,
                                   encoding: null,
                                   bufferSize: -1,
                                   leaveOpen: true);
            var points = hoverRoot["points"];
            var facades = hoverRoot["facades"];
            var edges = hoverRoot["edges"];


            WriteVerticesListAsync(streamWriter, points.AsObject());
            streamWriter.WriteLine();

            var groups = hoverRoot["groups"].AsObject();

            var maxPointIndex = 0;

            var groupFacades = new Dictionary<string, IEnumerable<int>>();

            //var opening = new[] { "opening_tops", "opening_sides", "opening_bottoms" };
            var opening = new[] { "outside_corners", "level_bases", "perimeter" };


            // streamWriter.WriteLine($"g opening");
            List<int> GroupFaces = new List<int>();



            foreach (var facade in facades.AsObject())
            {
                var edgesTypes = facade.Value["edges"].AsArray().Select(x => x.AsObject()["type"].ToString()).Distinct();


                foreach (var type in edgesTypes)
                {
                    streamWriter.WriteLine($"g {type}");

                    var edgesIds = facade.Value["edges"].AsArray()
                    .Where(x => x.AsObject()["type"].ToString() == type)
                    .Select(x => x.AsObject()["id"].ToString());

                    if (!edgesIds.Any())
                        continue;

                    var faces = edges.AsObject().Where(x => edgesIds.Contains(x.Key))

                          .Select(x => x.Value["points"]).SelectMany(x => x.AsArray())
                          .Select(x => pointIndexes[Convert.ToInt32(x.ToString())]);

                    streamWriter.WriteLine($"f {string.Join(' ', faces)}");

                    streamWriter.WriteLine();
                }

            }

            //  streamWriter.WriteLine($"f {string.Join(' ', faces)}");

            // BuildByGroups(streamWriter, facades, edges, groups, maxPointIndex);

            Console.WriteLine("Max point index " + maxPointIndex);
        }

        private static int BuildByGroups(StreamWriter streamWriter, JsonNode? facades, JsonNode? edges, JsonObject groups, int maxPointIndex)
        {
            foreach (var group in groups)
            {
                streamWriter.WriteLine($"g {group.Key}");

                var groupFacadeIds = group.Value["facade_ids"].AsArray();

                foreach (var groupFacadeId in groupFacadeIds)
                {
                    var groupFacade = facades.AsObject()[groupFacadeId.ToString()].AsObject();

                    var edgesIds = groupFacade["edges"].AsArray()
                        .Where(x => x.AsObject()["type"].ToString() != "opening_tops")
                        .Where(x => x.AsObject()["type"].ToString() != "opening_sides")
                        .Where(x => x.AsObject()["type"].ToString() != "opening_bottoms")

                        .Select(x => x.AsObject()["id"].ToString());

                    var faces = edges.AsObject().Where(x => edgesIds.Contains(x.Key))
                        .Select(x => x.Value["points"]).SelectMany(x => x.AsArray())
                        .Select(x => pointIndexes[Convert.ToInt32(x.ToString())]);

                    if (!faces.Any())
                        continue;

                    maxPointIndex = faces.Max();

                    streamWriter.WriteLine($"f {string.Join(' ', faces)}");
                }

                streamWriter.WriteLine();
            }

            return maxPointIndex;
        }

        private static void WriteVerticesListAsync(StreamWriter output, JsonObject vertices)
        {
            int i = 1;
            foreach (var vertex in vertices)
            {
                var position = vertex.Value["position"];

                output.WriteLine($"v {position[0].ToString().Replace('.', ',')} {position[1].ToString().Replace('.', ',')} {position[2].ToString().Replace('.', ',')}");

                pointIndexes.Add(Convert.ToInt32(vertex.Key), i);

                i++;
            }

            Console.WriteLine("Count of Vertix:" + vertices.Count());
        }
    }
}