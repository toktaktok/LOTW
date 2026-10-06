using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Framework;
using Project.Scripts.Data.Table;

namespace Tests.EditMode
{
    /// <summary>
    /// 테이블 밖의 '@키' (코드 리터럴, 프리팹, 씬, ScriptableObject)가 Text 테이블에 있는지,
    /// Localization.Resolve 를 거치는 Inspector 필드에 한국어 원문이 없는지 검사합니다.
    /// 테이블 컬럼은 변환기(Table/table_schema.py)가 검사합니다.
    /// </summary>
    public class TextKeyReferenceTests
    {
        private const string KeyPattern = @"@([a-z0-9_]+(?:\.[a-z0-9_]+)+)";
        // 따옴표로 닫힌 리터럴만. "@world.scene." + 이름 같은 이어붙이기는 검사하지 않음
        private static readonly Regex CodeKey = new("\"" + KeyPattern + "\"");
        // Unity YAML 은 '@' 로 시작하는 문자열을 작은따옴표로 저장. 키 형식이 틀린 값(@UI.Talk)도 잡도록 넓게 매칭
        private static readonly Regex YamlKey = new("'@([^'\\r\\n]+)'");

        // Localization.Resolve 를 거치는 Inspector 필드: 상호작용 프롬프트, CharacterProfile 이름,
        // MinigameDefinition 제목, 출구 라벨, LocalizedText 키. 직접 값과 프리팹 오버라이드 값
        private const string LocalizedFields = "(?:promptText|displayName|title|label|key)";
        private static readonly Regex FieldValue = new(@"^\s*-?\s*" + LocalizedFields + @": (.+)$", RegexOptions.Multiline);
        private static readonly Regex OverrideValue = new(@"propertyPath: (?:\S+\.)?" + LocalizedFields + @"\s*\n\s*value: (.+)$", RegexOptions.Multiline);
        // 한글 음절, 또는 YAML 이 이스케이프한 "가" 형태
        private static readonly Regex Korean = new(@"[가-힣]|\\u[A-Da-d][0-9A-Fa-f]{3}");

        private static readonly string[] YamlExtensions = { "*.prefab", "*.unity", "*.asset" };

        private HashSet<string> _keys;

        private static string ProjectDir => Path.Combine(Application.dataPath, "Project");

        [OneTimeSetUp]
        public void LoadKeys()
        {
            string tableDir = Path.Combine(ProjectDir, "Resources/Table");
            _keys = Directory.GetFiles(tableDir, "Text_*.json")
                .SelectMany(path => JsonArrayHelper.FromJson<TextData>(File.ReadAllText(path)))
                .Select(row => row.key)
                .ToHashSet();
        }

        [Test]
        public void KeyReferences_ExistInTextTables()
        {
            var missing = new List<string>();
            foreach(string path in Files("*.cs"))
                CollectMissing(path, CodeKey, missing);
            foreach(string path in YamlExtensions.SelectMany(Files))
                CollectMissing(path, YamlKey, missing);

            Assert.IsNotEmpty(_keys);
            Assert.IsEmpty(missing, "Text 테이블에 없는 키:\n" + string.Join("\n", missing));
        }

        [Test]
        public void LocalizedInspectorFields_HaveNoLiteralKorean()
        {
            var literals = new List<string>();
            foreach(string path in YamlExtensions.SelectMany(Files))
            {
                string text = File.ReadAllText(path);
                IEnumerable<Match> values = FieldValue.Matches(text).Concat(OverrideValue.Matches(text));
                foreach(Match match in values)
                {
                    if(Korean.IsMatch(match.Groups[1].Value))
                        literals.Add($"{Relative(path)}: {match.Value.Trim()}");
                }
            }

            Assert.IsEmpty(literals, "원문 대신 '@키'를 쓰세요 (Text_* 테이블에 문구 추가):\n" + string.Join("\n", literals));
        }

        private void CollectMissing(string path, Regex pattern, List<string> missing)
        {
            foreach(Match match in pattern.Matches(File.ReadAllText(path)))
            {
                if(!_keys.Contains(match.Groups[1].Value))
                    missing.Add($"{Relative(path)}: {match.Groups[1].Value}");
            }
        }

        /// <summary>Assets/Project 아래 파일. Art 폴더(폰트, 텍스처 에셋)는 건너뜁니다.</summary>
        private static IEnumerable<string> Files(string pattern)
        {
            string artDir = Path.Combine(ProjectDir, "Art");
            return Directory.GetFiles(ProjectDir, pattern, SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(artDir));
        }

        private static string Relative(string path) => path.Substring(Application.dataPath.Length + 1).Replace('\\', '/');
    }
}
