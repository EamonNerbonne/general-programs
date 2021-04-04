#ifndef SVGA_VESA_EMN
#define SVGA_VESA_EMN


#define R640x480
#define MAXX 639
#define MAXY 479
#define MODENUM 0x101
#define WIDTH 640
#define HEIGHT 480
#define NUMBANKS 5
#define vgapage ((uint8 *) 0xa0000)

#define max(a,b) (((a) > (b)) ? (a) : (b))
#define min(a,b) (((a) < (b)) ? (a) : (b))

typedef signed char int8;
typedef signed short int16;
typedef signed long int32;

typedef unsigned char uint8;
typedef unsigned short uint16;
typedef unsigned long uint32;

typedef float float32;
typedef double float64;

typedef struct
{
    uint16 red;
    uint16 green;
    uint16 blue;
} vgaColor;

typedef struct
{
    char vesa[4];
    uint8 minorMode;
    uint8 majorMode;
    char *vendorName;
    uint32 capabilities;
    uint16 *modes;
    uint16 memory;
    char reserved_236[236];
} vbeInfo;

typedef struct
{
    uint16 modeAttr;
    uint8 bankAAttr;
    uint8 bankBAttr;
    uint16 bankGranularity;
    uint16 bankSize;
    uint16 bankASegment;
    uint16 bankBSegment;
    uint32 posFuncPtr;
    uint16 bytesPerScanLine;
    uint16 width;
    uint16 height;
    uint8 charWidth;
    uint8 charHeight;
    uint8 numberOfPlanes;
    uint8 bitsPerPixel;
    uint8 numberOfBanks;
    uint8 memoryModel;
    uint8 videoBankSize;
    uint8 imagePages;

    uint8 reserved_1;

    uint8 redMaskSize;
    uint8 redFieldPos;
    uint8 greenMaskSize;
    uint8 greenFieldPos;
    uint8 blueMaskSize;
    uint8 blueFieldPos;
    uint8 rsvdMaskSize;
    uint8 rsvdFieldPos;
    uint8 DirectColorInfo;

    uint8 reserved_216[216];

} vbeModeInfo;

typedef enum
{
    FALSE = 0, TRUE = 1
} boolean;

typedef struct {
    int32 edi;
    int32 esi;
    int32 ebp;
    int32 reserved_by_system;
    int32 ebx;
    int32 edx;
    int32 ecx;
    int32 eax;
    int16 flags;
    int16 es,ds,fs,gs,ip,cs,sp,ss;
} rminfo;


       void   CloseMode(void);
       int32  InitMode(void);
       void   vgaSetPalette(int16 start, int16 count, vgaColor *p);
       int16  vbeSetBank(uint16 addr);
inline void   UpdateScr(char*buffer);
       void   line(int x1, int y1, int x2, int y2, char color,char*buffer);
inline void   plot_pixel(int x,int y,char c,char*buffer);


void extern MoveXk(char *vidbuf,int X);
#pragma aux MoveXk =   \
    "mov edi,0A0000h"   \
    "rep movsd"         \
    modify [edi esi ecx]\
    parm [esi] [ecx];

inline void UpdateScr(char*buffer)
{
    vbeSetBank(0);
    MoveXk(buffer,16384);
    buffer+=65536;

    vbeSetBank(1);
    MoveXk(buffer,16384);
    buffer+=65536;

    vbeSetBank(2);
    MoveXk(buffer,16384);
    buffer+=65536;

    vbeSetBank(3);
    MoveXk(buffer,16384);
    buffer+=65536;

    vbeSetBank(4);
    MoveXk(buffer,11264);
}

inline void plot_pixel(int x,int y,char c,char*buffer)
{
#ifdef R640x480
    *(buffer + x + (y<<9)+(y<<7))=c;
#else
    *(buffer + x + y*WIDTH)=c;
#endif
}

#endif
