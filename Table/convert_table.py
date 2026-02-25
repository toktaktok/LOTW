"""
convert_table.py
────────────────────────────────────────────────────────────────
Excel XML Spreadsheet 2003 (.xml) → JSON 변환 스크립트

사용법:
  python convert_table.py           # Excel/ 내 모든 .xml 변환
  python convert_table.py Dialogue  # DialogueTable.xml 만 변환

출력 경로 (두 곳 동시):
  Table/Json/                          ← 소스 관리용
  Assets/Project/Resources/Table/      ← Unity DataManager 로드 경로

헤더 규칙:
  - 1행 = 컬럼 헤더. C# 필드명에 맞게 작성 (첫 글자만 자동으로 소문자 변환)
    예) DataId → dataId, SpeakerName → speakerName
  - DataId 컬럼은 필수 (없는 행은 제외됨)
  - ChoiceIds 처럼 이름 끝이 'Ids'이고 값에 쉼표가 있으면 int 배열로 변환
  - 빈 셀은 null 처리 (JSON 에서 해당 키 생략)
"""

import os
import sys
import json
import xml.etree.ElementTree as ET

# ── 경로 설정 ──────────────────────────────────────────────────
SCRIPT_DIR   = os.path.dirname(os.path.abspath(__file__))
EXCEL_DIR    = os.path.join(SCRIPT_DIR, "Excel")
JSON_DIR     = os.path.join(SCRIPT_DIR, "Json")
UNITY_RES    = os.path.join(SCRIPT_DIR, "..", "Assets", "Project", "Resources", "Table")

OUTPUT_DIRS  = [JSON_DIR, UNITY_RES]

# Excel XML Spreadsheet 2003 네임스페이스
NS = "urn:schemas-microsoft-com:office:spreadsheet"


# ── 유틸 ──────────────────────────────────────────────────────
def to_camel(name: str) -> str:
    """첫 글자만 소문자로 변환 (DataID → dataID, SpeakerId → speakerId)"""
    return name[0].lower() + name[1:] if name else name


def parse_cell(cell) -> object:
    """셀 하나의 값을 Python 타입으로 변환합니다."""
    data = cell.find(f"{{{NS}}}Data")
    if data is None or not data.text:
        return None

    text = data.text.strip()
    dtype = data.get(f"{{{NS}}}Type", "String")

    if dtype == "Number":
        return float(text) if "." in text else int(text)
    if dtype == "Boolean":
        return text.lower() in ("1", "true", "yes")
    return text


def coerce_value(key: str, value):
    """
    특수 컬럼 후처리:
      - 이름 끝이 'Ids'이고 문자열에 쉼표가 있으면 int 배열로 변환
    """
    if value is None or value == "":
        return None

    lower = key.lower()
    if lower.endswith("ids") and isinstance(value, str) and "," in value:
        try:
            return [int(x.strip()) for x in value.split(",") if x.strip()]
        except ValueError:
            pass

    return value


# ── 파싱 ──────────────────────────────────────────────────────
def parse_xml(filepath: str) -> list[dict]:
    tree = ET.parse(filepath)
    root = tree.getroot()

    table = root.find(f".//{{{NS}}}Table")
    if table is None:
        raise ValueError("Table 요소를 찾을 수 없습니다.")

    rows = table.findall(f"{{{NS}}}Row")
    if len(rows) < 2:
        return []

    # 1행 = 헤더
    headers = []
    for cell in rows[0].findall(f"{{{NS}}}Cell"):
        data = cell.find(f"{{{NS}}}Data")
        headers.append(to_camel(data.text.strip()) if data is not None and data.text else "")

    # 2행~ = 데이터
    result = []
    for row in rows[1:]:
        cells   = row.findall(f"{{{NS}}}Cell")
        row_obj = {}
        col_idx = 0

        for cell in cells:
            # ss:Index 속성이 있으면 건너뛴 빈 셀을 고려
            idx_attr = cell.get(f"{{{NS}}}Index")
            if idx_attr is not None:
                col_idx = int(idx_attr) - 1

            if col_idx < len(headers) and headers[col_idx]:
                key   = headers[col_idx]
                value = coerce_value(key, parse_cell(cell))
                if value is not None:
                    row_obj[key] = value

            col_idx += 1

        # dataId 없는 행(빈 행, 주석 행 등) 제외
        if "dataId" in row_obj:
            result.append(row_obj)

    return result


# ── 출력 ──────────────────────────────────────────────────────
def ensure_dirs():
    for d in OUTPUT_DIRS:
        os.makedirs(d, exist_ok=True)


def write_json(table_name: str, data: list[dict]):
    json_str = json.dumps(data, ensure_ascii=False, indent=2)
    for out_dir in OUTPUT_DIRS:
        path = os.path.join(out_dir, f"{table_name}.json")
        with open(path, "w", encoding="utf-8") as f:
            f.write(json_str)
        print(f"    → {os.path.normpath(path)}")


# ── 메인 ──────────────────────────────────────────────────────
def convert(filter_name: str = ""):
    ensure_dirs()

    if not os.path.isdir(EXCEL_DIR):
        print(f"[오류] {EXCEL_DIR} 디렉토리가 없습니다.")
        return False

    xml_files = [
        f for f in os.listdir(EXCEL_DIR)
        if f.endswith(".xml") and (not filter_name or filter_name.lower() in f.lower())
    ]

    if not xml_files:
        print(f"[경고] 변환할 XML 파일이 없습니다 (필터: '{filter_name}').")
        return True

    ok = 0
    for xml_file in xml_files:
        table_name = os.path.splitext(xml_file)[0]
        xml_path   = os.path.join(EXCEL_DIR, xml_file)
        print(f"  [{xml_file}] 변환 중...")

        try:
            data = parse_xml(xml_path)
            write_json(table_name, data)
            print(f"    ✓ {len(data)}개 행 완료\n")
            ok += 1
        except Exception as e:
            print(f"    ✗ 오류: {e}\n")

    print(f"결과: {ok}/{len(xml_files)} 성공")
    return ok == len(xml_files)


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else ""
    success = convert(target)
    sys.exit(0 if success else 1)
