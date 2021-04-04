#ifndef EMNVGA
#define EMNVGA
#include <stdlib.h>
#include <conio.h>
#include <i86.h>

#define randomize() (srand(*((int*)0x46c)),(*((int*)0x46c)))

#define random(x) ((int)(rand()/32768.0*(x)))

#define SCREEN ((char*)0xA0000)
unsigned short RowOffset[200];

#define sgn(x) ((x<0)?-1:((x>0)?1:0)) /* macro to return the sign of a
                                         number */

void extern PutPixel(int X,int Y,int C,char*buffer);
#pragma aux PutPixel =         \
"       add edi, ecx   "      \
"       add ebx, ebx     "    \
"       mov cx, [ebx + RowOffset]"\
"       add edi, ecx"\
"       stosb" \
parm [ecx] [ebx] [eax] [edi];

char extern GetPixel(int X,int Y,char*buffer);
#pragma aux GetPixel =         \
"       add edi, ecx   "      \
"       add ebx, ebx     "    \
"       mov cx, [ebx + RowOffset]"\
"       add edi, ecx"\
"       mov eax, [edi]" \
parm [ecx] [ebx] [edi] \
value [al];



void extern UpdateScr(char *From,char*Into);
#pragma aux UpdateScr = \
    "mov ecx,3e80h"     \
    "rep movsd"         \
    modify [edi ecx]\
    parm [esi] [edi];

#define PAL_WRITE_ADDR (0x3c8)      // palette write address
#define PAL_READ_ADDR  (0x3c7)      // palette read address
#define PAL_DATA       (0x3c9)      // palette data register

typedef struct
{
    int r,g,b;
} ColorST;

void SetPalette(ColorST *p)
{
    int i;

    while(!(inp(0x3da) & 0x08));    // wait vertical retrace
    outp(PAL_WRITE_ADDR, 0);
    for (i = 0; i < 256; i++)
    {
        outp(PAL_DATA, p->r);
        outp(PAL_DATA, p->g);
        outp(PAL_DATA, p->b);
        p++;
    }
}

void SetMode()
{
  union REGS regs;
  int I;

  regs.w.ax = 0x13;
  int386(0x10, &regs, &regs);
  for (I=0;I<200;I++)
     RowOffset[I]=(unsigned short)(320*I);
}

void TextMode()
{
  union REGS regs;

  regs.w.ax = 0x3;
  int386(0x10, &regs, &regs);
}


void line(int x1, int y1, int x2, int y2, char color,char*buffer)
{
  int i,dx,dy,sdx,sdy,dxabs,dyabs,x,y,px,py;

  dx=x2-x1;      /* the horizontal distance of the line */
  dy=y2-y1;      /* the vertical distance of the line */
  dxabs=abs(dx);
  dyabs=abs(dy);
  sdx=sgn(dx);
  sdy=sgn(dy);
  x=dyabs>>1;
  y=dxabs>>1;
  px=x1;
  py=y1;

  PutPixel(px,py,color,buffer);

  if (dxabs>=dyabs) /* the line is more horizontal than vertical */
  {
    for(i=0;i<dxabs;i++)
    {
      y+=dyabs;
      if (y>=dxabs)
      {
        y-=dxabs;
        py+=sdy;
      }
      px+=sdx;
      PutPixel(px,py,color,buffer);
    }
  }
  else /* the line is more vertical than horizontal */
  {
    for(i=0;i<dyabs;i++)
    {
      x+=dxabs;
      if (x>=dyabs)
      {
        x-=dyabs;
        px+=sdx;
      }
      py+=sdy;
      PutPixel(px,py,color,buffer);
    }
  }
}
#endif
