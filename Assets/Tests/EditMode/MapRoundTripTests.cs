using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Data.Table;
using Project.Scripts.System.World.Map;

namespace Tests.EditMode
{
    public class MapRoundTripTests
    {
        [Test]
        public void RoundTrip_PreservesGraphAndIds()
        {
            var model = new MapModel { mapName = "rt" };
            int n0 = model.AddNode(new Vector3(0, 0, 0), Color.red, 0.3f);
            int n1 = model.AddNode(new Vector3(2, 0, 0), Color.green, 0.3f);
            int n2 = model.AddNode(new Vector3(4, 0, 0), Color.blue, 0.3f);
            Assert.IsTrue(model.AddEdge(n0, n1));
            Assert.IsTrue(model.AddEdge(n1, n2));

            var pe = new PlaceableEntry
            {
                prefabId = "NPC", position = new Vector3(1, 0, 0), eulerAngles = Vector3.zero,
                linkedNodeId = n1, entranceId = "", targetScene = "", targetEntranceId = ""
            };
            int oid = model.AddPlaceable(pe);

            string json = JsonUtility.ToJson(model.ToData(), true);
            MapData data = JsonUtility.FromJson<MapData>(json);
            MapModel rebuilt = MapModel.FromData(data);

            Assert.AreEqual(3, rebuilt.nodes.Count);
            Assert.AreEqual(2, rebuilt.edges.Count);
            Assert.AreEqual(model.NextId, rebuilt.NextId);
            Assert.IsTrue(rebuilt.HasEdge(n0, n1));
            Assert.IsTrue(rebuilt.HasEdge(n1, n2));
            Assert.AreEqual(1, rebuilt.placeables.Count);
            Assert.AreEqual(n1, rebuilt.placeables[0].linkedNodeId);
            Assert.AreEqual(oid, rebuilt.placeables[0].objectID);
        }

        [Test]
        public void LinkedNodeId_SurvivesAsMinusOne()
        {
            var model = new MapModel();
            model.AddNode(Vector3.zero, Color.white, 0.3f);
            model.AddPlaceable(new PlaceableEntry
            {
                prefabId = "Prop", linkedNodeId = -1, entranceId = "", targetScene = "", targetEntranceId = ""
            });

            string json = JsonUtility.ToJson(model.ToData(), true);
            MapModel rebuilt = MapModel.FromData(JsonUtility.FromJson<MapData>(json));
            Assert.AreEqual(-1, rebuilt.placeables[0].linkedNodeId);
        }

        [Test]
        public void AddEdge_RejectsThirdNeighbor()
        {
            var model = new MapModel();
            int a = model.AddNode(Vector3.zero, Color.white, 0.3f);
            int b = model.AddNode(Vector3.right, Color.white, 0.3f);
            int c = model.AddNode(Vector3.up, Color.white, 0.3f);
            int d = model.AddNode(Vector3.forward, Color.white, 0.3f);

            Assert.IsTrue(model.AddEdge(a, b));
            Assert.IsTrue(model.AddEdge(a, c));
            Assert.IsFalse(model.AddEdge(a, d));
        }

        [Test]
        public void Validate_FlagsBrokenMap()
        {
            var data = new MapData
            {
                mapName = "broken", nextId = 5,
                nodes = new[] { new NodeEntry { id = 1 }, new NodeEntry { id = 1 } },
                edges = new[] { new EdgeEntry { a = 1, b = 99 } },
                placeables = new[]
                {
                    new PlaceableEntry { prefabId = "", objectID = 7, linkedNodeId = 42 },
                    new PlaceableEntry { prefabId = "P", objectID = 7, linkedNodeId = -1 }
                }
            };
            Assert.IsNotEmpty(MapValidation.Validate(data));
        }

        [Test]
        public void Validate_PassesGoodMap()
        {
            var model = new MapModel();
            int a = model.AddNode(Vector3.zero, Color.white, 0.3f);
            int b = model.AddNode(Vector3.right, Color.white, 0.3f);
            model.AddEdge(a, b);
            Assert.IsEmpty(MapValidation.Validate(model.ToData()));
        }
    }
}
