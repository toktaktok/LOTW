"""
table_schema.py
────────────────────────────────────────────────────────────────
Schema/{테이블}.json 으로 표 그리드를 행 객체로 변환하고 검증합니다.

표 레이아웃 (B2 시작):
  1행 = 컬럼 이름 (C# 필드명, 첫 글자만 자동 소문자 변환)
  2행 = 자료형 (스키마 type 과 같아야 함)
  3행~ = 데이터. 완전히 빈 행은 건너뜀

분할 테이블:
  {스키마}_{분류}.xlsx 는 Schema/{스키마}.json 을 같이 쓰는 그룹입니다 (예: Text_UI, Text_Item).
  JSON 은 파일마다 따로 나오고, DataId 는 파일 안에서만 유일하면 됩니다.

스키마 컬럼 속성:
  name, type(int|float|bool|string|int[]), desc
  required     값 필수
  unique       true = 테이블 안에서 중복 금지, "group" = 같은 스키마 그룹 전체에서 중복 금지
  min/max      숫자 범위        maxCount 배열 최대 개수
  pattern      문자열 정규식 (전체 일치)
  prefixByPart 값이 "{분류 소문자}." 로 시작해야 함 (Text_UI → "ui.")
  ref          "스키마.컬럼" 그룹 안에 같은 값이 있어야 함 (allow 에 있는 값은 예외)
  textKey      '@키' 값이면 Text 그룹의 Key 에 있어야 함
  commandRefs  {"동사": "스키마.컬럼"}. ';' 로 나눈 "동사:키..." 명령의 키가 그 컬럼에 있어야 함
               (Dialogue Conditions/Actions 의 item:, giveItem: 같은 아이템 ID)
"""

import json
import os
import re

TYPES = ("int", "float", "bool", "string", "int[]")
INT_RE = re.compile(r"-?\d+")
# DialogueCommands 문법: [!]동사:키[연산자 값]
COMMAND_RE = re.compile(r"!?\s*([A-Za-z]+)\s*:\s*([^<>=!]+)")


class Table:
    def __init__(self, name, file):
        self.name = name
        self.file = file
        # Text_UI → 스키마 Text, 분류 UI. 밑줄이 없으면 분류 없음
        self.schema_name, _, self.part = name.partition("_")
        self.origin_row = 1  # 표 헤더의 엑셀 행 번호 (1-based)
        self.origin_col = 1  # 표 첫 컬럼의 엑셀 열 번호 (1-based)
        self.rows = []                # (엑셀 행 번호, {camelKey: 값})
        self.col_pos = {}             # 컬럼 이름 → 표 안 열 인덱스
        self.schema = None


def to_camel(name: str) -> str:
    """첫 글자만 소문자로 변환. DataId → dataId, SpeakerName → speakerName."""
    return name[0].lower() + name[1:] if name else name


def col_letter(col: int) -> str:
    letters = ""
    while col:
        col, rem = divmod(col - 1, 26)
        letters = chr(65 + rem) + letters
    return letters


def load_schema(schema_dir: str, table_name: str):
    path = os.path.join(schema_dir, f"{table_name}.json")
    if not os.path.isfile(path):
        return None
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# ── 값 변환 ───────────────────────────────────────────────────
def coerce(value, type_name: str):
    """셀 값을 스키마 자료형으로 변환. 실패하면 ValueError."""
    if type_name == "string":
        return str(value)
    if type_name == "int":
        if isinstance(value, int) and not isinstance(value, bool):
            return value
        if isinstance(value, str) and INT_RE.fullmatch(value):
            return int(value)
    if type_name == "float":
        if isinstance(value, (int, float)) and not isinstance(value, bool):
            return float(value)
        if isinstance(value, str):
            try:
                return float(value)
            except ValueError:
                pass
    if type_name == "bool":
        if isinstance(value, bool):
            return value
        if str(value).lower() in ("true", "1"):
            return True
        if str(value).lower() in ("false", "0"):
            return False
    if type_name == "int[]":
        if isinstance(value, int) and not isinstance(value, bool):
            return [value]
        parts = [p.strip() for p in str(value).split(",") if p.strip()]
        if parts and all(INT_RE.fullmatch(p) for p in parts):
            return [int(p) for p in parts]
    raise ValueError(f"'{value}' 를 {type_name} 로 읽을 수 없습니다")


# ── 그리드 → 행 ──────────────────────────────────────────────
def build_rows(table: Table, grid: list, errors: list):
    """grid[0]=헤더, grid[1]=자료형, grid[2:]=데이터. 헤더/자료형이 스키마와 다르면 errors 에 추가."""
    schema_cols = {c["name"]: c for c in table.schema["columns"]}
    for col in schema_cols.values():
        if col["type"] not in TYPES:
            errors.append(f"Schema/{table.schema_name}.json: {col['name']} 의 type '{col['type']}' 은 지원하지 않습니다 ({', '.join(TYPES)})")
    if len(grid) < 2:
        errors.append(f"{table.file}: 헤더와 자료형 행이 필요합니다")
        return

    def cell(row_offset, i):
        return f"{table.file} {col_letter(table.origin_col + i)}{table.origin_row + row_offset}"

    headers = [str(v).strip() if v is not None else "" for v in grid[0]]
    for missing in [n for n in schema_cols if n not in headers]:
        errors.append(f"{table.file}: 스키마 컬럼 '{missing}' 이 표에 없습니다")

    columns = []  # (열 인덱스, 스키마 컬럼)
    for i, header in enumerate(headers):
        if not header:
            continue
        col = schema_cols.get(header)
        if col is None:
            errors.append(f"{cell(0, i)}: '{header}' 는 스키마에 없는 컬럼입니다")
            continue
        type_cell = str(grid[1][i]).strip() if grid[1][i] is not None else ""
        if type_cell != col["type"]:
            errors.append(f"{cell(1, i)}: 자료형 '{type_cell}' 이 스키마 '{col['type']}' 와 다릅니다")
        columns.append((i, col))
        table.col_pos[header] = i

    for offset, values in enumerate(grid[2:], start=2):
        if all(v is None for v in values):
            continue
        row = {}
        for i, col in columns:
            raw = values[i]
            if raw is None:
                continue
            try:
                row[to_camel(col["name"])] = coerce(raw, col["type"])
            except ValueError as e:
                errors.append(f"{cell(offset, i)}: {e}")
        table.rows.append((table.origin_row + offset, row))


# ── 검증 ─────────────────────────────────────────────────────
def validate(tables: dict) -> list:
    """모든 테이블의 행을 스키마 규칙으로 검사해 오류 메시지 목록을 반환합니다."""
    errors = []

    def values_of(ref: str) -> set:
        schema_name, col_name = ref.split(".")
        key = to_camel(col_name)
        return {r.get(key) for t in tables.values() if t.schema_name == schema_name for _, r in t.rows}

    text_keys = values_of("Text.Key")
    group_seen = {}  # (스키마, 컬럼) → {값: "파일 행"}

    for table in tables.values():
        for col in table.schema["columns"]:
            key = to_camel(col["name"])
            allow = col.get("allow", [])
            ref_values = values_of(col["ref"]) if "ref" in col else None
            letter = col_letter(table.origin_col + table.col_pos.get(col["name"], 0))
            seen = group_seen.setdefault((table.schema_name, key), {}) if col.get("unique") == "group" else {}
            prefix = f"{table.part.lower()}." if col.get("prefixByPart") and table.part else None
            command_refs = {verb.lower(): (ref, values_of(ref)) for verb, ref in col.get("commandRefs", {}).items()}
            for row_num, row in table.rows:
                where = f"{table.file} {letter}{row_num}({col['name']})"
                value = row.get(key)
                if value is None:
                    if col.get("required"):
                        errors.append(f"{where}: 값이 필요합니다")
                    continue
                items = value if isinstance(value, list) else [value]

                if col.get("unique"):
                    if value in seen:
                        errors.append(f"{where}: '{value}' 가 {seen[value]}과 중복됩니다")
                    seen.setdefault(value, f"{table.file} {row_num}행")
                if "pattern" in col and not re.fullmatch(col["pattern"], str(value)):
                    errors.append(f"{where}: '{value}' 가 형식 {col['pattern']} 과 맞지 않습니다")
                if prefix and not str(value).startswith(prefix):
                    errors.append(f"{where}: '{value}' 는 '{prefix}' 로 시작해야 합니다 ({table.file})")
                if "maxCount" in col and len(items) > col["maxCount"]:
                    errors.append(f"{where}: 최대 {col['maxCount']}개까지 가능합니다 ({len(items)}개)")
                for item in items:
                    if item in allow:
                        continue
                    if "min" in col and item < col["min"]:
                        errors.append(f"{where}: {item} 은 최소값 {col['min']} 보다 작습니다")
                    if "max" in col and item > col["max"]:
                        errors.append(f"{where}: {item} 은 최대값 {col['max']} 보다 큽니다")
                    if ref_values is not None and item not in ref_values:
                        errors.append(f"{where}: {item} 이 {col['ref']} 에 없습니다")
                if command_refs and isinstance(value, str):
                    for token in value.split(";"):
                        match = COMMAND_RE.match(token.strip())
                        if not match or match.group(1).lower() not in command_refs:
                            continue
                        ref, ref_keys = command_refs[match.group(1).lower()]
                        command_key = match.group(2).strip()
                        if command_key not in ref_keys:
                            errors.append(f"{where}: '{token.strip()}' 의 '{command_key}' 가 {ref} 에 없습니다")
                if col.get("textKey") and isinstance(value, str) and value.startswith("@"):
                    if value[1:] not in text_keys:
                        errors.append(f"{where}: Text 키 '{value[1:]}' 가 없습니다")
    return errors
