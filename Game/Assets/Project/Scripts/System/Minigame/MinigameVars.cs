using System.Collections.Generic;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 미니게임 한 판 동안만 사는 정수 변수. 메카닉이 쓰고, 정의의 결과 조건(var:key)이 읽습니다.
    /// </summary>
    public class MinigameVars
    {
        private readonly Dictionary<string, int> _values = new();

        public int Get(string key) => _values.TryGetValue(key, out int value) ? value : 0;
        public void Set(string key, int value) => _values[key] = value;
        public void Add(string key, int amount = 1) => _values[key] = Get(key) + amount;
    }
}
