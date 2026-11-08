from pathlib import Path
from docx import Document
from docx.shared import Inches
from docx.oxml.ns import qn

path=Path(__file__).parent/'EggPress3D_Report_Updated.docx'
d=Document(path)
for table in d.tables:
    table.autofit=False
    widths=[col.width for col in table.columns]
    available=Inches(6.25)
    total=sum(widths)
    widths=[int(w*available/total) for w in widths]
    for col,width in zip(table.columns,widths): col.width=width
    for row in table.rows:
        for cell,width in zip(row.cells,widths): cell.width=width
    width=table._tbl.tblPr.find(qn('w:tblW'))
    if width is not None:
        width.set(qn('w:type'),'dxa'); width.set(qn('w:w'),'9000')
d.save(path)
