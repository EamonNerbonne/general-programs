#include <stdlib.h>
#include <conio.h>

#define WIDTH 320
#define HEIGHT 200
#define HS 150
#define MIN 32
#define MX 31

#define randomize() (srand(*((int*)0x46c)),(*((int*)0x46c)))

#define random(x) ((int)(rand()/32768.0*(x)))

#define SCREEN ((char*)0xA0000)
void extern ClearXl(char*Into,int What,int X);//clears X long's, i.e. 4*X bytes. Fast...
#pragma aux ClearXl =   \
    "rep stosd"         \
    parm [edi] [eax] [ecx];

void extern MoveXl(char *From,char*Into,int longs);//move X longs
#pragma aux MoveXl = \
    "rep movsd"         \
    modify [edi ecx]\
    parm [esi] [edi] [ecx];

void extern outpE(int addr,int what);
#pragma aux outpE = \
   "  out dx, al "\
   parm [edx] [eax];


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

//    while(!(inp(0x3da) & 0x08));    // wait vertical retrace
    outpE(PAL_WRITE_ADDR, 0);
    for (i = 0; i < 256; i++)
    {
        outpE(PAL_DATA, p->r);
        outpE(PAL_DATA, p->g);
        outpE(PAL_DATA, p->b);
        p++;
    }
}

void extern SetMode(void);
#pragma aux SetMode = \
    "mov  eax, 0x13"   \
    "int  0x10"       \
    modify [eax];
    
void extern TextMode(void);
#pragma aux TextMode = \
    "mov  eax, 0x3"   \
    "int  0x10"       \
    modify [eax];

void main(void)
{
    char*buffer;
    int hotspots[HS];
    int x;
    int i=0;
    SetMode();
    for (x=0;x<HS;x++)
      hotspots[x]=(rand()&255)+32;
    buffer=(char*)malloc(WIDTH*HEIGHT);

    ColorST palette[256];
    for(i=0;i<64;i++)
    {
        palette[i].r=i/2;
        palette[i].g=0  ;
        palette[i].b=0  ;
        palette[i+64].r=32+i/4;
        palette[i+64].g=i/2;
        palette[i+64].b=0  ;
        palette[i+128].r=48+i/8;
        palette[i+128].g=32+i/4;
        palette[i+128].b=i/2;
        palette[i+192].r=56+i/8;
        palette[i+192].g=48+i/4;
        palette[i+192].b=32+i/2;
    }
    SetPalette(palette);
    i=0;
    
    while(!kbhit())
    {
        ClearXl(buffer+197*WIDTH,0,240);
        for (x=0;x<HS;x++)
        {
          hotspots[x]+=(rand()&7)-3 -(i^=1);
          if ((hotspots[x]>288)||(hotspots[x]<32)) hotspots[x]=32+(rand()&255);
          buffer[197*WIDTH + hotspots[x]]=255;
          buffer[198*WIDTH + hotspots[x]]=255;
          buffer[199*WIDTH + hotspots[x]]=255;
        }
        for(x=0;x<WIDTH*(HEIGHT-3);x++)
//9         buffer[x]=(char)((buffer[x+319] + buffer[x+320] + buffer[x+641] + buffer[x+960]>>2|1)-1);
//8         buffer[x]=(char)((buffer[x+320] + buffer[x+640] + buffer[x+641] + buffer[x+639]>>2|1)-1);
//7         buffer[x]=(char)((buffer[x] + buffer[x+320] + buffer[x+641] + buffer[x+959]>>2|1)-1);
//6         buffer[x]=(char)((buffer[x+1] + buffer[x+320] + buffer[x+640] + buffer[x+959]>>2|1)-1);
//5         buffer[x]=(char)((buffer[x+641] + buffer[x+319] + buffer[x+1] + buffer[x+960]>>2|1)-1);
//4         buffer[x]=(char)((buffer[x+321] + buffer[x+640] + buffer[x+319] + buffer[x+960]>>2|1)-1);
//3         buffer[x]=(char)((buffer[x+321] + buffer[x+320] + buffer[x+639] + buffer[x+640]>>2|1)-1);
//2         buffer[x]=(char)((buffer[x+321] + buffer[x+320] + buffer[x+639] + buffer[x+960]>>2|2)-2);
//1         buffer[x]=(char)((buffer[x+321] + buffer[x+320] + buffer[x+639] + buffer[x+960]+1>>2|2)-2);
//0         buffer[x]=(char)((buffer[x+321] + buffer[x+320] + buffer[x+639] + buffer[x+960]+3>>2|2)-2);
        MoveXl(buffer,SCREEN,16000-240);
    }
    TextMode();
}
