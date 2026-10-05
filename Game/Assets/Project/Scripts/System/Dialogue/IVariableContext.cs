namespace Project.Scripts.System.Dialogue
{
    /// <summary>
    /// 조건 'var:key' 가 읽는 임시 변수 창구. 미니게임처럼 세션 동안만 사는 값을 조건식에 노출합니다.
    /// IDialogueContext 구현이 이 인터페이스도 구현할 때만 var: 조건을 쓸 수 있습니다.
    /// </summary>
    public interface IVariableContext
    {
        int GetVar(string key);
    }
}
