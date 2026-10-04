"""
convert_table.py
────────────────────────────────────────────────────────────────
Excel (.xlsx / XML Spreadsheet 2003 .xml) → JSON 변환 스크립트

사용법:
  python convert_table.py           # Excel/ 내 모든 .xlsx, .xml 변환
  python convert_table.py Dialogue  # Dialogue.xlsx (또는 .xml) 만 변환

.xlsx 변환에는 openpyxl 이 필요합니다:  pip install openpyxl

출력 경로 (두 곳 동시):
  Table/Json/                          ← 소스 관리용
  Game/Assets/Project/Resources/Table/ ← Unity DataManager 로드 경로

표(AutoFilter) 규칙:
  - Excel에서 데이터 범위를 선택 후 [삽입 > 표]로 반드시 표를 정의해야 함
  - 표의 첫 번째 행 = 컬럼 헤더 (C# 필드명, 첫 글자만 자동 소문자 변환)
    예) DataId → dataId, SpeakerName → speakerName
  - 표 범위 바깥의 셀은 무시됨 (메모, 설명 등을 자유롭게 작성 가능)
  - dataId 는 반드시 정수여야 함 — 문자열·공백·비어있는 행은 자동 제외
  - 이름 끝이 'Ids'인 컬럼은 int 배열로 변환 ("2,3" → [2, 3], 2 → [2])
"""

import os
import sys
import re
import json
import xml.etree.ElementTree as ET

# ── 경로 설정 ──────────────────────────────────────────────────
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
EXCEL_DIR  = os.path.join(SCRIPT_DIR, "Excel")
JSON_DIR   = os.path.join(SCRIPT_DIR, "Json")
UNITY_RES  = os.path.join(SCRIPT_DIR, "..", "Game", "Assets", "Project", "Resources", "Table")

OUTPUT_DIRS = [JSON_DIR, UNITY_RES]

# Excel XML Spreadsheet 2003 네임스페이스
NS  = "urn:schemas-microsoft-com:office:spreadsheet"
NSX = "urn:schemas-microsoft-com:office:excel"


# ── R1C1 범위 파싱 ─────────────────────────────────────────────
def parse_rc_range(raw: str):
    """
    R1C1:R6C6 형식을 (start_row, start_col, end_row, end_col) 로 변환.
    '=Dialogue!R1C1:R6C6' 처럼 시트 참조나 '=' 접두사가 있어도 처리합니다.
    """
    s = raw.strip().lstrip("=")
    if "!" in s:
        s = s.split("!")[-1]
    m = re.fullmatch(r"R(\d+)C(\d+):R(\d+)C(\d+)", s)
    if not m:
        return None
    return int(m.group(1)), int(m.group(2)), int(m.group(3)), int(m.group(4))


# ── 행 인덱스 구성 ─────────────────────────────────────────────
def build_row_map(table_elem) -> dict:
    """
    <Table> 안의 <Row> 요소를 행 번호(1-based) → element 로 매핑합니다.
    ss:Index 속성이 있는 행은 그 번호를 사용하고, 없으면 순서대로 증가합니다.
    """
    row_map = {}
    row_idx = 0
    for row_elem in table_elem.findall(f"{{{NS}}}Row"):
        idx_attr = row_elem.get(f"{{{NS}}}Index")
        row_idx  = int(idx_attr) if idx_attr else row_idx + 1
        row_map[row_idx] = row_elem
    return row_map


# ── 열 범위 내 셀 추출 ─────────────────────────────────────────
def get_cells_in_range(row_elem, start_col: int, end_col: int) -> dict:
    """
    행 요소에서 열 범위[start_col, end_col](1-based, 포함) 내 셀을
    {col_num: element} 딕셔너리로 반환합니다.
    ss:Index 속성으로 건너뛴 열도 정확하게 처리합니다.
    """
    cells = {}
    col_idx = 0
    for cell in row_elem.findall(f"{{{NS}}}Cell"):
        idx_attr = cell.get(f"{{{NS}}}Index")
        col_idx  = int(idx_attr) if idx_attr else col_idx + 1
        if start_col <= col_idx <= end_col:
            cells[col_idx] = cell
        if col_idx >= end_col:
            break
    return cells


# ── 셀 값 파싱 ────────────────────────────────────────────────
def parse_cell(cell) -> object:
    """셀 하나의 값을 Python 타입으로 반환합니다. 빈 셀이면 None."""
    data = cell.find(f"{{{NS}}}Data")
    if data is None or not data.text:
        return None
    text  = data.text.strip()
    dtype = data.get(f"{{{NS}}}Type", "String")
    if dtype == "Number":
        return float(text) if "." in text else int(text)
    if dtype == "Boolean":
        return text.lower() in ("1", "true", "yes")
    return text


# ── 값 후처리 ─────────────────────────────────────────────────
def coerce_value(key: str, value):
    """
    특수 컬럼 후처리:
      - 이름 끝이 'ids'이면 int 배열로 변환 (C# int[] 필드와 매칭)
        예) "2,3" → [2, 3], 2 → [2]
    """
    if value is None or value == "":
        return None
    if key.lower().endswith("ids"):
        if isinstance(value, int) and not isinstance(value, bool):
            return [value]
        if isinstance(value, str):
            try:
                return [int(x.strip()) for x in value.split(",") if x.strip()]
            except ValueError:
                pass
    return value


# ── 헤더 이름 변환 ─────────────────────────────────────────────
def to_camel(name: str) -> str:
    """첫 글자만 소문자로 변환. DataId → dataId, SpeakerName → speakerName."""
    return name[0].lower() + name[1:] if name else name


# ── XML 파싱 메인 ─────────────────────────────────────────────
def parse_xml(filepath: str) -> list:
    tree = ET.parse(filepath)
    root = tree.getroot()

    worksheet = root.find(f".//{{{NS}}}Worksheet")
    if worksheet is None:
        raise ValueError("Worksheet 요소를 찾을 수 없습니다.")

    table_elem = worksheet.find(f"{{{NS}}}Table")
    if table_elem is None:
        raise ValueError("Table 요소를 찾을 수 없습니다.")

    # ── 표(AutoFilter) 범위 읽기 ──────────────────────────────
    auto_filter = worksheet.find(f"{{{NSX}}}AutoFilter")
    if auto_filter is None:
        raise ValueError(
            "워크시트에 '표(Table)'가 정의되어 있지 않습니다.\n"
            "  Excel에서 데이터 범위를 선택한 뒤 [삽입 > 표]를 클릭하여\n"
            "  표를 만든 후 XML로 저장하세요."
        )

    raw_range = (
        auto_filter.get(f"{{{NSX}}}Range") or
        auto_filter.get("Range") or ""
    )
    if not raw_range:
        raise ValueError("AutoFilter에 Range 속성이 없습니다.")

    table_range = parse_rc_range(raw_range)
    if table_range is None:
        raise ValueError(f"표 범위 형식을 인식할 수 없습니다: '{raw_range}'")

    sr, sc, er, ec = table_range  # start_row, start_col, end_row, end_col (1-based)

    # ── 표 범위를 값 그리드로 변환 ────────────────────────────
    row_map = build_row_map(table_elem)
    grid = []
    for row_num in range(sr, er + 1):
        row_elem = row_map.get(row_num)
        cells = get_cells_in_range(row_elem, sc, ec) if row_elem is not None else {}
        grid.append([parse_cell(cells[c]) if c in cells else None for c in range(sc, ec + 1)])

    return grid_to_rows(grid)


# ── XLSX 파싱 메인 ────────────────────────────────────────────
def parse_xlsx(filepath: str) -> list:
    try:
        from openpyxl import load_workbook
        from openpyxl.utils import range_boundaries
    except ImportError:
        raise RuntimeError("openpyxl 이 없습니다. 'pip install openpyxl' 후 다시 실행하세요.")

    wb = load_workbook(filepath, data_only=True)
    table, sheet = None, None
    for ws in wb.worksheets:
        if ws.tables:
            sheet = ws
            table = next(iter(ws.tables.values()))
            break

    if table is None:
        raise ValueError(
            "워크시트에 '표(Table)'가 정의되어 있지 않습니다.\n"
            "  Excel에서 데이터 범위를 선택한 뒤 [삽입 > 표]를 클릭하여 표를 만드세요."
        )

    min_col, min_row, max_col, max_row = range_boundaries(table.ref)
    grid = []
    for row in sheet.iter_rows(min_row=min_row, max_row=max_row,
                               min_col=min_col, max_col=max_col, values_only=True):
        grid.append([normalize_xlsx_value(v) for v in row])

    return grid_to_rows(grid)


def normalize_xlsx_value(value):
    """openpyxl 셀 값을 XML 파서와 같은 형태로 맞춥니다. 빈 셀이면 None."""
    if value is None:
        return None
    if isinstance(value, str):
        value = value.strip()
        return value or None
    if isinstance(value, float) and value.is_integer():
        return int(value)
    return value


# ── 그리드 → 행 객체 ──────────────────────────────────────────
def grid_to_rows(grid: list) -> list:
    """
    grid[0] = 헤더 행, grid[1:] = 데이터 행 (표 범위 내 값만, 빈 셀은 None).
    """
    if not grid:
        return []

    headers = [to_camel(str(v).strip()) if v is not None else "" for v in grid[0]]

    result = []
    for values in grid[1:]:
        row_obj = {}
        for key, raw in zip(headers, values):
            if not key:
                continue
            value = coerce_value(key, raw)
            if value is not None:
                row_obj[key] = value

        # dataId 는 반드시 정수여야 함
        # — 문자열, 공백, 누락된 행은 읽을 데이터가 아니므로 제외
        data_id = row_obj.get("dataId")
        if not isinstance(data_id, int):
            continue

        result.append(row_obj)

    return result


# ── 출력 ──────────────────────────────────────────────────────
def ensure_dirs():
    for d in OUTPUT_DIRS:
        os.makedirs(d, exist_ok=True)


def write_json(table_name: str, data: list):
    json_str = json.dumps(data, ensure_ascii=False, indent=2)
    for out_dir in OUTPUT_DIRS:
        path = os.path.join(out_dir, f"{table_name}.json")
        with open(path, "w", encoding="utf-8") as f:
            f.write(json_str)
        print(f"    → {os.path.normpath(path)}")


# ── 메인 ──────────────────────────────────────────────────────
def convert(filter_name: str = "") -> bool:
    ensure_dirs()

    if not os.path.isdir(EXCEL_DIR):
        print(f"[오류] {EXCEL_DIR} 디렉토리가 없습니다.")
        return False

    parsers = {".xlsx": parse_xlsx, ".xml": parse_xml}

    # '~$' 로 시작하는 파일은 Excel 이 열려 있을 때 생기는 잠금 파일
    table_files = [
        f for f in os.listdir(EXCEL_DIR)
        if os.path.splitext(f)[1].lower() in parsers
        and not f.startswith("~$")
        and (not filter_name or filter_name.lower() in f.lower())
    ]

    if not table_files:
        print(f"[경고] 변환할 테이블 파일이 없습니다 (필터: '{filter_name}').")
        return True

    ok = 0
    for table_file in table_files:
        table_name, ext = os.path.splitext(table_file)
        table_path = os.path.join(EXCEL_DIR, table_file)
        print(f"  [{table_file}] 변환 중...")

        try:
            data = parsers[ext.lower()](table_path)
            write_json(table_name, data)
            print(f"    ✓ {len(data)}개 행 완료\n")
            ok += 1
        except Exception as e:
            print(f"    ✗ 오류: {e}\n")

    print(f"결과: {ok}/{len(table_files)} 성공")
    return ok == len(table_files)


if __name__ == "__main__":
    target  = sys.argv[1] if len(sys.argv) > 1 else ""
    success = convert(target)
    sys.exit(0 if success else 1)
