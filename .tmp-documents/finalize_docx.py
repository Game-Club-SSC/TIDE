from copy import deepcopy
from pathlib import Path

from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn


SOURCE = Path(__file__).with_name("TIDE_Game_Vision_and_Production_Brief.headers.docx")
OUTPUT = Path(__file__).with_name("TIDE_Game_Vision_and_Production_Brief.final.docx")


def set_table_indent(table, twips: int) -> None:
    properties = table._tbl.tblPr
    existing = properties.find(qn("w:tblInd"))
    if existing is not None:
        properties.remove(existing)
    indent = OxmlElement("w:tblInd")
    indent.set(qn("w:w"), str(twips))
    indent.set(qn("w:type"), "dxa")
    width = properties.find(qn("w:tblW"))
    if width is not None:
        width.addnext(indent)
    else:
        properties.insert(0, indent)


document = Document(SOURCE)
for table in document.tables:
    set_table_indent(table, 120)

document.save(OUTPUT)
print(OUTPUT)
