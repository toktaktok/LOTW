#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Scripts.Core;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// Dialogue 테이블 편집기와 분기 시뮬레이션 (Docs/DialogueEditor.md).
    /// 왼쪽: 블록 그래프 (노드 추가/삭제, 연결). 오른쪽: 선택한 행 편집, 시뮬레이션.
    /// 저장하면 Table/table_edit.py 가 바뀐 행만 Dialogue.xlsx, Text_Dialogue.xlsx 에 쓰고 JSON 으로 변환합니다.
    /// 새로고침은 convert_table.py 로 엑셀을 다시 변환해 읽습니다 (엑셀에서 직접 고친 내용 반영).
    /// </summary>
    public class DialogueEditorWindow : EditorWindow
    {
        private static readonly string[] EditedTables = { ToolDefines.DialogueTableName, ToolDefines.DialogueTextTable };

        [SerializeField] private int block = -1;
        [SerializeField] private DialogueTableSource source;
        // 읽어 온 시점의 엑셀 수정 시각 (EditedTables 순서). 저장 전에 엑셀 쪽 변경을 알아채는 데 씀
        [SerializeField] private long[] excelWriteTicks;

        private DialogueGraphView _graph;
        private ToolbarMenu _blockMenu;
        private ToolbarButton _saveButton;
        private DialogueSimulator _simulator;
        private CommandPickers _pickers;
        private GUIStyle _textAreaStyle;
        private int _selectedId = -1;
        private int _startId = -1;
        private string _newKey = string.Empty;
        private string _resultMessage;
        private bool _resultFailed;
        private bool _showSettings;
        private Vector2 _scroll;

        [MenuItem("LOTW/Dialogue Editor")]
        private static void Open()
        {
            GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        // 창에 포커스가 있을 때만 동작하고, 그동안은 전역 Ctrl+S(씬 저장)보다 먼저 받음
        [Shortcut("LOTW/Dialogue Editor/Save", typeof(DialogueEditorWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void SaveShortcut(ShortcutArguments args)
        {
            ((DialogueEditorWindow)args.context).Save();
        }

        private void OnEnable()
        {
            saveChangesMessage = "Dialogue 편집 내용을 엑셀에 저장할까요?";
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnDestroy()
        {
            if(source != null)
                DestroyImmediate(source);
        }

        private void CreateGUI()
        {
            var toolbar = new Toolbar();
            _blockMenu = new ToolbarMenu();
            toolbar.Add(_blockMenu);
            toolbar.Add(new ToolbarButton(() => AddLine(false)) { text = "대사 추가" });
            toolbar.Add(new ToolbarButton(() => AddLine(true)) { text = "분기 추가" });
            _saveButton = new ToolbarButton(() => Save()) { text = "저장" };
            toolbar.Add(_saveButton);
            toolbar.Add(new ToolbarButton(Refresh) { text = "새로고침" });
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(1, ToolDefines.DialogueSimPanelWidth, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;
            _graph = new DialogueGraphView();
            _graph.OnStartRequested += StartSimulation;
            _graph.OnNodeSelected += id => { _selectedId = id; Repaint(); };
            _graph.OnEdited += RefreshGraph;
            _graph.OnMoved += SaveLayout;
            split.Add(_graph);
            split.Add(new IMGUIContainer(DrawPanel));
            rootVisualElement.Add(split);

            // 도메인 리로드 뒤에는 편집 중인 source 를 그대로 씀
            if(source == null)
                LoadSource();
            InitView();
        }

        public override void SaveChanges()
        {
            if(Save())
                base.SaveChanges();
        }

        #region Load and save

        private void LoadSource()
        {
            if(source != null)
                DestroyImmediate(source);
            source = DialogueTableSource.Load();
            excelWriteTicks = EditedTables.Select(t => TableWriter.GetWriteTime(t).Ticks).ToArray();
        }

        private void InitView()
        {
            _simulator = new DialogueSimulator(id => source.GetLine(id));
            _pickers = new CommandPickers(source);
            _graph.Positions = DialogueLayout.Load();

            List<int> blocks = source.GetBlocks().ToList();
            if(blocks.Count == 0)
            {
                _blockMenu.text = "Dialogue.json 이 비어 있습니다";
                return;
            }
            ShowBlock(blocks.Contains(block) ? block : blocks[0]);
        }

        private void Refresh()
        {
            if(source.IsDirty && !EditorUtility.DisplayDialog("새로고침", "저장하지 않은 편집 내용이 사라집니다.", "새로고침", "취소"))
                return;

            (bool success, string output) = TableWriter.Run(ToolDefines.TableConvertScript);
            SetResult(success ? null : output, !success);
            AssetDatabase.Refresh();
            LoadSource();
            InitView();
        }

        private bool Save()
        {
            string json = DialogueEdits.BuildSaveJson(source);
            if(json == null)
            {
                ShowNotification(new GUIContent("바뀐 내용이 없습니다"));
                return true;
            }

            string locked = EditedTables.FirstOrDefault(TableWriter.IsLocked);
            if(locked != null)
            {
                SetResult($"{locked}.xlsx 가 엑셀에서 열려 있습니다. 엑셀을 닫고 다시 저장하세요.", true);
                return false;
            }

            bool excelChanged = EditedTables.Where((t, i) => excelWriteTicks == null || TableWriter.GetWriteTime(t).Ticks != excelWriteTicks[i]).Any();
            if(excelChanged && !EditorUtility.DisplayDialog("엑셀이 바뀌었습니다",
                   "툴에서 읽은 뒤에 엑셀 파일이 수정되었습니다. 저장하면 툴에서 고친 행이 엑셀의 같은 행을 덮어씁니다. 다른 행은 그대로 둡니다.", "저장", "취소"))
                return false;

            File.WriteAllText(ToolDefines.DialogueEditsTempPath, json);
            (bool success, string output) = TableWriter.Run(ToolDefines.TableEditScript, Path.GetFullPath(ToolDefines.DialogueEditsTempPath));
            SetResult(output, !success);
            if(!success)
                return false;

            AssetDatabase.Refresh();
            LoadSource();
            InitView();
            SaveLayout();
            return true;
        }

        private void SaveLayout()
        {
            DialogueLayout.Save(_graph.Positions, source.Lines.Select(l => l.dataId).ToHashSet());
        }

        private void SetResult(string message, bool failed)
        {
            _resultMessage = message;
            _resultFailed = failed;
            Repaint();
        }

        #endregion

        #region Graph

        private void ShowBlock(int newBlock)
        {
            block = newBlock;
            _graph.Show(source, newBlock, true);

            List<DialogueData> roots = source.GetBlockRoots(newBlock);
            _startId = roots.Count > 0 ? roots[0].dataId : -1;
            UpdateViewState();
        }

        /// <summary>편집이나 Undo 뒤에 그래프를 다시 그립니다. 화면 위치와 선택은 유지합니다.</summary>
        private void RefreshGraph()
        {
            _graph.Show(source, block, false);
            UpdateViewState();
        }

        private void UpdateViewState()
        {
            _graph.Select(_selectedId);
            _graph.SetCurrent(_simulator.Current != null ? _simulator.Current.dataId : -1);
            RebuildBlockMenu();
            hasUnsavedChanges = source.IsDirty;
            _saveButton.text = hasUnsavedChanges ? "저장 *" : "저장";
            Repaint();
        }

        private void RebuildBlockMenu()
        {
            for(int i = _blockMenu.menu.MenuItems().Count - 1; i >= 0; i--)
                _blockMenu.menu.RemoveItemAt(i);
            foreach(int b in source.GetBlocks())
                _blockMenu.menu.AppendAction(GetBlockLabel(b), _ => ShowBlock(b), _ => b == block ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            _blockMenu.menu.AppendSeparator();
            _blockMenu.menu.AppendAction("새 대화", _ => AddBlock());
            _blockMenu.text = GetBlockLabel(block);
        }

        private string GetBlockLabel(int b)
        {
            DialogueData speakerLine = source.GetBlockLines(b).FirstOrDefault(l => !string.IsNullOrEmpty(l.speakerId) && l.speakerId != ToolDefines.DialoguePlayerSpeakerId);
            string speaker = speakerLine != null ? speakerLine.speakerId : "?";
            return $"{b * ToolDefines.DialogueBlockSize}번대  {speaker}";
        }

        private void AddLine(bool isRouter)
        {
            if(!_graph.AddLine(null, isRouter))
                ShowNotification(new GUIContent($"{block * ToolDefines.DialogueBlockSize}번대가 가득 찼습니다"));
        }

        /// <summary>마지막 블록 다음 번호로 새 대화를 열고 첫 행을 만듭니다.</summary>
        private void AddBlock()
        {
            int next = source.GetBlocks().DefaultIfEmpty(0).Max() + 1;
            ShowBlock(next);
            _graph.AddLine(Vector2.zero);
        }

        private void OnUndoRedo()
        {
            if(source == null || _graph == null)
                return;
            source.MarkChanged();
            RefreshGraph();
        }

        #endregion

        private void DrawPanel()
        {
            if(_simulator == null)
                return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if(!string.IsNullOrEmpty(_resultMessage))
            {
                EditorGUILayout.HelpBox(_resultMessage, _resultFailed ? MessageType.Error : MessageType.Info);
                if(GUILayout.Button("닫기", GUILayout.ExpandWidth(false)))
                    _resultMessage = null;
                EditorGUILayout.Space();
            }
            DrawInspector();
            DrawStartState();
            EditorGUILayout.Space();
            DrawProgress();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.EndScrollView();
        }

        #region Inspector

        private void DrawInspector()
        {
            DialogueData line = _selectedId >= 0 ? source.GetLine(_selectedId) : null;
            if(line == null)
                return;

            EditorGUILayout.LabelField($"행 {line.dataId}", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            // 이미 쓰인 화자 중에서 고르면 ID 와 이름을 같이 바꿈
            List<(string id, string name)> speakers = source.Lines.Where(l => !string.IsNullOrEmpty(l.speakerId))
                .Select(l => (l.speakerId, l.speakerName ?? string.Empty)).Distinct().OrderBy(p => p.speakerId).ToList();
            int speakerIndex = speakers.IndexOf((line.speakerId, line.speakerName ?? string.Empty));
            int picked = EditorGUILayout.Popup("화자", speakerIndex, speakers.Select(p => $"{p.id}  {source.Resolve(p.name)}").ToArray());
            string speakerId = EditorGUILayout.TextField("화자 ID", line.speakerId);
            string speakerName = EditorGUILayout.TextField("화자 이름", line.speakerName);
            if(picked != speakerIndex)
                (speakerId, speakerName) = speakers[picked];
            string text = EditorGUILayout.TextField("대사 키", line.text);
            int nextId = EditorGUILayout.IntField("다음", line.nextId);
            string choices = EditorGUILayout.TextField("선택지", line.choiceIds != null ? string.Join(",", line.choiceIds) : string.Empty);
            string conditions = CommandListField.Draw("조건", line.conditions, true, _pickers);
            string actions = CommandListField.Draw("실행", line.actions, false, _pickers);
            if(EditorGUI.EndChangeCheck())
            {
                DialogueEdits.Record(source, "행 편집", () =>
                {
                    line.speakerId = speakerId;
                    line.speakerName = speakerName;
                    line.text = text;
                    line.nextId = nextId;
                    line.choiceIds = ParseIds(choices);
                    line.conditions = conditions;
                    line.actions = actions;
                });
                RefreshGraph();
            }

            DrawLineText(line);
            EditorGUILayout.Space();
        }

        private void DrawLineText(DialogueData line)
        {
            if(string.IsNullOrEmpty(line.text))
            {
                EditorGUILayout.HelpBox("대사가 없는 분기 행입니다. 후보 중 조건을 만족하는 첫 행으로 바로 넘어갑니다.", MessageType.None);
                return;
            }

            TextData text = Localization.IsKey(line.text) ? source.GetDialogueText(line.text.Substring(1)) : null;
            if(text == null)
            {
                EditorGUILayout.LabelField(source.Resolve(line.text), EditorStyles.wordWrappedLabel);
                EditorGUILayout.HelpBox("Text_Dialogue 에 없는 문구라 여기서 고칠 수 없습니다.", MessageType.None);
                if(GUILayout.Button("이 행 전용 대사로 분리"))
                    EditAndRefresh(() => DialogueEdits.SplitText(source, line));
                return;
            }

            int uses = source.CountTextUses(line.text);
            if(uses > 1)
            {
                EditorGUILayout.HelpBox($"이 대사는 {uses}개 행이 같이 씁니다. 고치면 모두 바뀝니다.", MessageType.Warning);
                if(GUILayout.Button("이 행 전용 대사로 분리"))
                    EditAndRefresh(() => DialogueEdits.SplitText(source, line));
            }

            _textAreaStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            EditorGUI.BeginChangeCheck();
            string ko = EditorGUILayout.TextArea(text.ko, _textAreaStyle);
            if(EditorGUI.EndChangeCheck())
                EditAndRefresh(() => DialogueEdits.SetText(source, text.key, ko));
        }

        private void EditAndRefresh(Action edit)
        {
            edit();
            RefreshGraph();
        }

        private static int[] ParseIds(string text)
        {
            return text.Split(',').Select(s => int.TryParse(s.Trim(), out int id) ? id : -1).Where(id => id >= 0).ToArray();
        }

        #endregion

        #region Simulation

        private void StartSimulation(int startId)
        {
            _startId = startId;
            _simulator.Start(startId);
            OnSimulationStep();
        }

        private void OnSimulationStep()
        {
            DialogueData current = _simulator.Current;
            if(current != null && DialogueTableSource.GetBlock(current.dataId) != block)
                ShowBlock(DialogueTableSource.GetBlock(current.dataId));
            _graph.SetCurrent(current != null ? current.dataId : -1);
            Repaint();
        }

        private void DrawStartState()
        {
            EditorGUILayout.LabelField("시작 상태", EditorStyles.boldLabel);
            DrawStartValues("플래그", _simulator.StartFlags);
            DrawStartValues("아이템", _simulator.StartItems);

            EditorGUILayout.BeginHorizontal();
            _newKey = EditorGUILayout.TextField(_newKey);
            if(GUILayout.Button("+ 플래그", GUILayout.ExpandWidth(false)))
                AddStartKey(_simulator.StartFlags);
            if(GUILayout.Button("+ 아이템", GUILayout.ExpandWidth(false)))
                AddStartKey(_simulator.StartItems);
            EditorGUILayout.EndHorizontal();

            if(GUILayout.Button("이 블록 조건에 쓰인 키 채우기"))
                FillConditionKeys();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _startId = EditorGUILayout.IntField("시작 행", _startId);
            if(GUILayout.Button("시작", GUILayout.ExpandWidth(false)))
                StartSimulation(_startId);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStartValues(string label, Dictionary<string, int> values)
        {
            foreach(string key in values.Keys.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                values[key] = EditorGUILayout.IntField($"{label}  {key}", values[key]);
                if(GUILayout.Button("x", GUILayout.ExpandWidth(false)))
                    values.Remove(key);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddStartKey(Dictionary<string, int> values)
        {
            string key = _newKey.Trim();
            if(key.Length > 0 && !values.ContainsKey(key))
                values[key] = 0;
            _newKey = string.Empty;
            GUI.FocusControl(null);
        }

        private void FillConditionKeys()
        {
            var flags = new HashSet<string>();
            var items = new HashSet<string>();
            DialogueSimulator.CollectConditionKeys(source.GetBlockLines(block), flags, items);
            foreach(string key in flags)
                _simulator.StartFlags.TryAdd(key, 0);
            foreach(string key in items)
                _simulator.StartItems.TryAdd(key, 0);
        }

        private void DrawProgress()
        {
            EditorGUILayout.LabelField("진행", EditorStyles.boldLabel);
            if(!_simulator.IsRunning)
            {
                EditorGUILayout.HelpBox("시작 행을 정하고 [시작]을 누르거나, 노드를 우클릭해 시작합니다.", MessageType.Info);
                return;
            }

            DialogueData current = _simulator.Current;
            if(current == null)
            {
                EditorGUILayout.HelpBox("대화 종료", MessageType.None);
            }
            else
            {
                string speaker = source.Resolve(string.IsNullOrEmpty(current.speakerName) ? current.speakerId : current.speakerName);
                EditorGUILayout.LabelField($"[{current.dataId}] {speaker}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(source.Resolve(current.text), EditorStyles.wordWrappedLabel);
                DrawChoices();
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = current != null && !_simulator.HasAvailableChoice;
            if(GUILayout.Button("다음"))
                Step(_simulator.Advance);
            GUI.enabled = _simulator.CanStepBack;
            if(GUILayout.Button("뒤로"))
                Step(_simulator.StepBack);
            GUI.enabled = true;
            if(GUILayout.Button("처음부터"))
                Step(_simulator.Restart);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
            foreach(KeyValuePair<string, int> pair in _simulator.Context.Flags)
                EditorGUILayout.LabelField($"플래그  {pair.Key}", pair.Value.ToString());
            foreach(KeyValuePair<string, int> pair in _simulator.Context.Items)
                EditorGUILayout.LabelField($"아이템  {pair.Key}", pair.Value.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기록", EditorStyles.boldLabel);
            foreach(string entry in _simulator.Context.Log)
                EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawChoices()
        {
            foreach(DialogueSimChoice choice in _simulator.Choices)
            {
                string text = choice.line != null ? source.Resolve(choice.line.text) : string.Empty;
                GUI.enabled = choice.IsAvailable;
                if(GUILayout.Button($"[{choice.dataId}] {text}"))
                {
                    int id = choice.dataId;
                    Step(() => _simulator.Choose(id));
                }
                GUI.enabled = true;

                if(!choice.IsAvailable)
                {
                    Color color = GUI.contentColor;
                    GUI.contentColor = ToolDefines.DialogueBlockedColor;
                    EditorGUILayout.LabelField($"    숨김: {choice.blockedBy}", EditorStyles.miniLabel);
                    GUI.contentColor = color;
                }
            }
        }

        private void Step(Action action)
        {
            action();
            OnSimulationStep();
            GUIUtility.ExitGUI();
        }

        #endregion

        private void DrawSettings()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "설정");
            if(!_showSettings)
                return;

            EditorGUI.BeginChangeCheck();
            string path = EditorGUILayout.TextField("Python 경로", TableWriter.PythonPath);
            if(EditorGUI.EndChangeCheck())
                TableWriter.PythonPath = path;
            EditorGUILayout.HelpBox("비워 두면 %LOCALAPPDATA%/Programs/Python 설치본, 없으면 PATH 의 python 을 씁니다. openpyxl 이 필요합니다.", MessageType.None);
        }
    }
}
#endif
