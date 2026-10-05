using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Data;

namespace Tests.EditMode
{
    /// <summary>
    /// Table/Schema/*.json 의 컬럼과 자료형이 C# RowData 필드와 같은지 검사합니다.
    /// RowData 필드를 바꾸면 스키마와 엑셀 자료형 행도 함께 바꿔야 합니다.
    /// </summary>
    public class TableSchemaTests
    {
        [Serializable]
        private class SchemaFile
        {
            public string rowClass;
            public SchemaColumn[] columns;
        }

        [Serializable]
        private class SchemaColumn
        {
            public string name;
            public string type;
        }

        private static readonly Dictionary<string, Type> TypeMap = new()
        {
            { "int", typeof(int) },
            { "float", typeof(float) },
            { "bool", typeof(bool) },
            { "string", typeof(string) },
            { "int[]", typeof(int[]) },
        };

        private static string SchemaDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Table/Schema"));

        private static IEnumerable<string> SchemaFiles() =>
            Directory.Exists(SchemaDir) ? Directory.GetFiles(SchemaDir, "*.json") : Array.Empty<string>();

        [Test]
        public void SchemaFolder_HasSchemas()
        {
            Assert.IsNotEmpty(SchemaFiles(), $"{SchemaDir} 에 스키마가 없습니다");
        }

        [TestCaseSource(nameof(SchemaFiles))]
        public void Schema_MatchesRowDataFields(string path)
        {
            var schema = JsonUtility.FromJson<SchemaFile>(File.ReadAllText(path));
            Type rowType = typeof(TableRowData).Assembly.GetType($"Project.Scripts.Data.Table.{schema.rowClass}");
            Assert.IsNotNull(rowType, $"{Path.GetFileName(path)}: rowClass {schema.rowClass} 가 없습니다");

            Dictionary<string, Type> fields = rowType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(f => f.Name, f => f.FieldType);
            Dictionary<string, SchemaColumn> columns = schema.columns
                .ToDictionary(c => char.ToLowerInvariant(c.name[0]) + c.name.Substring(1));

            CollectionAssert.AreEquivalent(fields.Keys, columns.Keys, $"{Path.GetFileName(path)}: 컬럼과 {schema.rowClass} 필드가 다릅니다");
            foreach(KeyValuePair<string, SchemaColumn> pair in columns)
            {
                Assert.IsTrue(TypeMap.TryGetValue(pair.Value.type, out Type type), $"{pair.Value.name}: 지원하지 않는 type {pair.Value.type}");
                Assert.AreEqual(fields[pair.Key], type, $"{Path.GetFileName(path)}: {pair.Value.name} 자료형이 다릅니다");
            }
        }
    }
}
