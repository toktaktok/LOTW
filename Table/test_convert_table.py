"""
convert_table / table_schema 검증 테스트.
실행: python -m unittest test_convert_table   (Table 폴더에서)
"""

import json
import os
import tempfile
import unittest

import convert_table
from table_schema import Table, build_rows, validate

SCHEMAS = {
    "Dialogue": {"columns": [
        {"name": "DataId", "type": "int", "required": True, "unique": True, "min": 1},
        {"name": "Text", "type": "string", "textKey": True},
        {"name": "NextId", "type": "int", "ref": "Dialogue.DataId", "allow": [-1]},
        {"name": "ChoiceIds", "type": "int[]", "ref": "Dialogue.DataId", "maxCount": 2},
    ]},
    "Text": {"columns": [
        {"name": "DataId", "type": "int", "required": True, "unique": True},
        {"name": "Key", "type": "string", "required": True, "unique": True},
    ]},
}
HEADER = ["DataId", "Text", "NextId", "ChoiceIds"]
TYPES = ["int", "string", "int", "int[]"]
TEXT_GRID = [["DataId", "Key"], ["int", "string"], [1, "hello"]]


def run(dialogue_grid):
    tables, errors = {}, []
    for name, grid in (("Dialogue", dialogue_grid), ("Text", TEXT_GRID)):
        table = Table(name, f"{name}.xlsx")
        table.origin_row, table.origin_col = 2, 2
        table.schema = SCHEMAS[name]
        build_rows(table, grid, errors)
        tables[name] = table
    return tables, errors + validate(tables)


class ValidateTests(unittest.TestCase):
    def test_valid_rows_convert(self):
        tables, errors = run([HEADER, TYPES, [1, "@hello", 2, None], [2, "hi", -1, "1,2"], [None] * 4])
        self.assertEqual(errors, [])
        self.assertEqual([r for _, r in tables["Dialogue"].rows],
                         [{"dataId": 1, "text": "@hello", "nextId": 2}, {"dataId": 2, "text": "hi", "nextId": -1, "choiceIds": [1, 2]}])

    def test_each_rule_reports_error(self):
        cases = {
            "자료형 'string'": [HEADER, ["string", "string", "int", "int[]"], [1, "a", -1, None]],
            "스키마에 없는 컬럼": [HEADER + ["Extra"], TYPES + ["int"], [1, "a", -1, None, 0]],
            "읽을 수 없습니다": [HEADER, TYPES, [1, "a", "x", None]],
            "값이 필요합니다": [HEADER, TYPES, [None, "a", -1, None]],
            "중복": [HEADER, TYPES, [1, "a", -1, None], [1, "b", -1, None]],
            "최소값": [HEADER, TYPES, [0, "a", -1, None]],
            "Dialogue.DataId 에 없습니다": [HEADER, TYPES, [1, "a", 9, None]],
            "최대 2개": [HEADER, TYPES, [1, "a", -1, "1,1,1"]],
            "Text 키": [HEADER, TYPES, [1, "@missing", -1, None]],
        }
        for expected, grid in cases.items():
            _, errors = run(grid)
            self.assertTrue(any(expected in e for e in errors), f"{expected}: {errors}")

    def test_error_points_at_cell(self):
        _, errors = run([HEADER, TYPES, [1, "a", "x", None]])
        self.assertIn("Dialogue.xlsx D4", errors[0])
        _, errors = run([HEADER, TYPES, [1, "a", 9, None]])
        self.assertIn("Dialogue.xlsx D4(NextId)", errors[0])


class CommandRefTests(unittest.TestCase):
    """Dialogue Conditions/Actions 의 아이템 ID 가 Item 테이블에 있는지 (commandRefs)."""

    def run_commands(self, actions):
        schemas = {
            "Dialogue": {"columns": [
                {"name": "DataId", "type": "int"},
                {"name": "Actions", "type": "string", "commandRefs": {"giveItem": "Item.ItemId", "item": "Item.ItemId"}},
            ]},
            "Item": {"columns": [{"name": "ItemId", "type": "string"}]},
        }
        grids = {
            "Dialogue": [["DataId", "Actions"], ["int", "string"], [1, actions]],
            "Item": [["ItemId"], ["string"], ["rose"]],
        }
        tables, errors = {}, []
        for name, grid in grids.items():
            table = Table(name, f"{name}.xlsx")
            table.origin_row, table.origin_col = 2, 2
            table.schema = schemas[name]
            build_rows(table, grid, errors)
            tables[name] = table
        return errors + validate(tables)

    def test_known_item_passes(self):
        self.assertEqual(self.run_commands("giveItem:rose=2;setFlag:tulip;!item:rose>=1"), [])

    def test_missing_item_reports_error(self):
        errors = self.run_commands("setFlag:got;giveitem:tulip=1")
        self.assertEqual(len(errors), 1)
        self.assertIn("'tulip' 가 Item.ItemId 에 없습니다", errors[0])


class SplitTableTests(unittest.TestCase):
    """Text_UI, Text_Item 처럼 Schema/Text.json 을 같이 쓰는 분할 테이블."""

    SCHEMA = {"columns": [
        {"name": "DataId", "type": "int", "required": True, "unique": True},
        {"name": "Key", "type": "string", "unique": "group", "pattern": "[a-z0-9_]+(\\.[a-z0-9_]+)+", "prefixByPart": True},
    ]}

    def run_split(self, ui_rows, item_rows, dialogue_text="@ui.talk"):
        tables, errors = {}, []
        grids = {
            "Text_UI": [["DataId", "Key"], ["int", "string"]] + ui_rows,
            "Text_Item": [["DataId", "Key"], ["int", "string"]] + item_rows,
            "Dialogue": [HEADER, TYPES, [1, dialogue_text, -1, None]],
        }
        for name, grid in grids.items():
            table = Table(name, f"{name}.xlsx")
            table.schema = SCHEMAS["Dialogue"] if name == "Dialogue" else self.SCHEMA
            build_rows(table, grid, errors)
            tables[name] = table
        return errors + validate(tables)

    def test_data_id_is_per_file_and_text_key_spans_group(self):
        errors = self.run_split([[1, "ui.talk"]], [[1, "item.rose.name"]], "@item.rose.name")
        self.assertEqual(errors, [])

    def test_group_rules(self):
        cases = {
            "Text_UI.xlsx 3행과 중복": ([[1, "ui.talk"]], [[1, "ui.talk"]]),
            "'item.' 로 시작해야": ([[1, "ui.talk"]], [[1, "ui.rose"]]),
            "형식": ([[1, "ui.Talk"]], []),
        }
        for expected, (ui_rows, item_rows) in cases.items():
            errors = self.run_split(ui_rows, item_rows)
            self.assertTrue(any(expected in e for e in errors), f"{expected}: {errors}")


class ConvertTests(unittest.TestCase):
    def test_error_stops_without_writing_json(self):
        from openpyxl import Workbook
        from openpyxl.worksheet.table import Table as XlTable

        with tempfile.TemporaryDirectory() as tmp:
            excel, schema, out = (os.path.join(tmp, d) for d in ("Excel", "Schema", "Out"))
            os.makedirs(excel)
            os.makedirs(schema)
            for name, grid in (("Dialogue", [HEADER, TYPES, [1, "a", 9, None]]), ("Text", TEXT_GRID)):
                with open(os.path.join(schema, f"{name}.json"), "w", encoding="utf-8") as f:
                    json.dump(SCHEMAS[name], f)
                wb = Workbook()
                for r, values in enumerate(grid, start=2):
                    for c, v in enumerate(values, start=2):
                        wb.active.cell(r, c, v)
                ref = f"B2:{chr(65 + len(grid[0]))}{len(grid) + 1}"
                wb.active.add_table(XlTable(displayName=name, ref=ref))
                wb.save(os.path.join(excel, f"{name}.xlsx"))

            saved = convert_table.EXCEL_DIR, convert_table.SCHEMA_DIR, convert_table.OUTPUT_DIRS
            convert_table.EXCEL_DIR, convert_table.SCHEMA_DIR, convert_table.OUTPUT_DIRS = excel, schema, [out]
            try:
                self.assertFalse(convert_table.convert())
            finally:
                convert_table.EXCEL_DIR, convert_table.SCHEMA_DIR, convert_table.OUTPUT_DIRS = saved
            self.assertEqual(os.listdir(out), [])


if __name__ == "__main__":
    unittest.main()
