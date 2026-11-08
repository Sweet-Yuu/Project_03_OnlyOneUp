from pathlib import Path
from docx import Document
from PIL import Image, ImageOps, ImageDraw
from io import BytesIO

out = Path(__file__).parent / 'qa'
out.mkdir(exist_ok=True)
d = Document(r'C:/Users/HN/Downloads/EggPress3D_Report.docx')
items = []
for i,p in enumerate(d.paragraphs):
    for blip in p._p.xpath('.//a:blip'):
        rid = blip.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}embed')
        part = d.part.related_parts[rid]
        im = Image.open(BytesIO(part.blob)).convert('RGB')
        path = out / f'source-P{i}.png'
        im.save(path)
        items.append((i,im))
for batch in range(0,len(items),6):
    canvas = Image.new('RGB',(1400,1020),'#eeeeee')
    draw=ImageDraw.Draw(canvas)
    for k,(pi,im) in enumerate(items[batch:batch+6]):
        x=(k%2)*700; y=(k//2)*340
        thumb=ImageOps.contain(im,(680,305))
        canvas.paste(thumb,(x+10,y+30)); draw.text((x+10,y+8),f'Paragraph {pi}',fill='black')
    canvas.save(out/f'sources-{batch//6+1}.png')
print('Source images at paragraphs:',[i for i,_ in items])
