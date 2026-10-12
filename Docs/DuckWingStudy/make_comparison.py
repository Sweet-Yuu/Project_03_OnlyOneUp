from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
P=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',27)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',21)
im=Image.new('RGB',(1530,1640),(28,32,39)); d=ImageDraw.Draw(im)
for c,(pose,title) in enumerate([('closed','KHÉP • Ban đầu'),('medium','MỞ VỪA • Wing 35° / Tip 12°'),('wide','MỞ RỘNG • Wing 58° / Tip 12°')]):
    d.text((c*510+12,12),title,font=small,fill='white')
    for r,(view,label) in enumerate([('front','TRƯỚC'),('side','BÊN HÔNG'),('back','SAU LƯNG')]):
        img=Image.open(P/f'{pose}_{view}.png').convert('RGB').resize((500,500))
        im.paste(img,(c*510+5,70+r*520))
        ImageDraw.Draw(im).text((c*510+18,80+r*520),label,font=small,fill='white')
im.save(P/'comparison_3x3.jpg',quality=94)
im=Image.new('RGB',(1500,550),(28,32,39)); d=ImageDraw.Draw(im)
for c,(pose,title) in enumerate([('closed','Khép: sải 2,001'),('wide','Mở rộng: sải 3,084'),('tip_excess','WingTip 40°: lông cong, không xòe quạt')]):
    d.text((c*500+12,12),title,font=small,fill='white')
    im.paste(Image.open(P/f'{pose}_front.png').convert('RGB').resize((500,500)),(c*500,50))
im.save(P/'before_after.jpg',quality=94)
