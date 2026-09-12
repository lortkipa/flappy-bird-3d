"""Generate an original alpha-cutout cedar frond without external packages."""
import math, random, struct, zlib, os
random.seed(19)
S=512
pixels=bytearray(S*S*4)
def line(a,b,width,color):
    ax,ay=a;bx,by=b;dx=bx-ax;dy=by-ay;den=dx*dx+dy*dy
    for y in range(max(0,int(min(ay,by)-width-1)),min(S,int(max(ay,by)+width+2))):
        for x in range(max(0,int(min(ax,bx)-width-1)),min(S,int(max(ax,bx)+width+2))):
            t=max(0,min(1,((x-ax)*dx+(y-ay)*dy)/max(den,.001)))
            d=math.hypot(x-ax-t*dx,y-ay-t*dy)
            alpha=max(0,min(1,width*(1-t*.65)+.8-d))
            i=(y*S+x)*4
            if alpha*255>pixels[i+3]:pixels[i:i+4]=bytes((*color,int(alpha*255)))
line((256,502),(256,20),3,(86,91,47))
for j in range(25):
    y=470-j*17
    length=190*(1-j/28)**.65
    for side in [-1,1]:
        start=(256,y+random.uniform(-8,8));end=(256+side*length,y-60-random.uniform(0,25))
        line(start,end,1.7,(66,79,36))
        for k in range(28):
            t=k/28
            px=start[0]+(end[0]-start[0])*t;py=start[1]+(end[1]-start[1])*t
            for direction in [-1,1]:
                n=random.uniform(12,27)*(1-t*.4)
                color=random.choice([(44,83,52),(58,103,60),(79,115,64),(96,126,67),(43,76,47)])
                line((px,py),(px+side*n*.8,py+direction*n*.65-8),random.uniform(1,1.8),color)
def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
raw=b''.join(b'\0'+pixels[y*S*4:(y+1)*S*4] for y in range(S))
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',S,S,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b'')
root=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.makedirs(root+'/Assets/Resources/Textures',exist_ok=True)
open(root+'/Assets/Resources/Textures/Cedar.png','wb').write(png)
print('Generated cedar frond texture')
