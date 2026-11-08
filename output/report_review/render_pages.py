from pathlib import Path
import pypdfium2 as pdfium
from pypdf import PdfReader
from PIL import Image, ImageOps, ImageDraw

root=Path(__file__).parent/'qa'
pdf=root/'EggPress3D_Report_Updated.pdf'
reader=PdfReader(pdf)
doc=pdfium.PdfDocument(pdf)
imgs=[]
for i,page in enumerate(doc):
    im=page.render(scale=1.5).to_pil().convert('RGB')
    im.save(root/f'page-{i+1:02}.png')
    imgs.append(im)
texts=[]
for i,page in enumerate(reader.pages):
    txt=page.extract_text()
    texts.append(f'PAGE {i+1}\n{txt}')
    print(i+1,len(txt),txt[:95].replace('\n',' / '))
(root/'pages.txt').write_text('\n\n'.join(texts),encoding='utf-8')
for base in range(0,len(imgs),6):
    canvas=Image.new('RGB',(1500,1440),'#cccccc')
    draw=ImageDraw.Draw(canvas)
    for k,im in enumerate(imgs[base:base+6]):
        x=k%3*500;y=k//3*720
        thumb=ImageOps.contain(im,(490,685))
        canvas.paste(thumb,(x+5,y+28))
        draw.text((x+10,y+8),f'Page {base+k+1}',fill='black')
    canvas.save(root/f'contact-{base//6+1:02}.png')
