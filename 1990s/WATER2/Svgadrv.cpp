#include <i86.h>
#include <string.h>
#include <stdlib.h>
#include <conio.h>
#include "svgadrv.hpp"

static vbeModeInfo modeInfo;
static vbeInfo info;
static int32 currentBank;




//---------------------------------------------------
//
// Ask dos for memory in the low part of memory
//

static uint32
allocDosMem(int16 size, uint32 *segment, uint32 *selector)
{
    union REGS reg;

    reg.w.ax = 0x0100;
    reg.w.bx = uint16((size + 15) >> 4);

    int386(0x31, &reg, &reg);

    if (reg.x.cflag)
    {
        return 0;
    }

    *segment = reg.w.ax;
    *selector = reg.w.dx;
    return (uint32) ((reg.w.ax & 0xffff) << 4);
}

//---------------------------------------------------
//
// free memory allocated using allocDosMem()
//

static void
freeDosMem(uint32 selector)
{
    union REGS reg;

    reg.w.ax = 0x0101;
    reg.w.dx = uint16(selector);

    int386(0x31, &reg, &reg);
}

//---------------------------------------------------
//
// Convert a real mode pointer into a protected mode
// pointer. 
//

static uint32
rmp2pmp(uint32 addr)
{
    return ((addr & 0xffff0000) >> 12) + (addr & 0xffff);
}

//---------------------------------------------------
//
// use DPMI translation services to simulate a real
// mode interrupt
//

static inline
int386rm(uint16 inter, rminfo *inout)
{
    union REGS reg;
    struct SREGS sreg;

    segread(&sreg);
    memset(&reg, 0, sizeof(reg));

    reg.w.ax = 0x0300;
    reg.h.bl = uint8(inter);
    reg.h.bh = 0;
    reg.w.cx = 0;
    sreg.es = FP_SEG(inout);
    reg.x.edi = FP_OFF(inout);

    int386x(0x31, &reg, &reg, &sreg);
}


static int16
vbeGetInfo(vbeInfo *infoPtr)
{
    rminfo rmi;

    uint32 dosmem;
    uint32 seg;
    uint32 sel;

    int16 len;
    uint16 *modes;

    dosmem = allocDosMem(sizeof(vbeInfo), &seg, &sel);

    memset(&rmi, 0, sizeof(rmi));
    rmi.eax = 0x4f00;
    rmi.es = uint16(dosmem >> 4);
    rmi.edi = 0;

    int386rm(0x10, &rmi);

    memcpy(infoPtr, (void *)dosmem, sizeof(vbeInfo));
    freeDosMem(sel);

    if (rmi.eax == 0x004f)
    {
        infoPtr->vendorName = 
            strdup((char *)rmp2pmp((uint32)infoPtr->vendorName));

        modes = (uint16 *) rmp2pmp((uint32)infoPtr->modes);
        len = 0;

        while ((*modes) != 0xffff)
        {
            len++;
            modes++;
        }
        modes = (uint16 *) rmp2pmp((uint32)infoPtr->modes);
        infoPtr->modes = (uint16 *)malloc(sizeof(uint16) * (len + 1));
        memcpy(infoPtr->modes, modes, sizeof(uint16) * (len + 1));

        return TRUE;
    }

    return FALSE;
}

static int16
vbeGetModeInfo(int16 mode, vbeModeInfo *infoPtr)
{
    union REGS reg;
    struct SREGS sreg;
    rminfo rmi;

    uint32 dosmem;
    uint32 seg;
    uint32 sel;

    dosmem = allocDosMem(sizeof(vbeModeInfo), &seg, &sel);

    segread(&sreg);
    memset(&reg, 0, sizeof(reg));

    memset(&rmi, 0, sizeof(rmi));
    rmi.eax = 0x4f01;
    rmi.ecx = mode;
    rmi.es = uint16(dosmem >> 4);
    rmi.edi = 0;

    int386rm(0x10, &rmi);

    memcpy(infoPtr, (void *)dosmem, sizeof(vbeModeInfo));
    freeDosMem(sel);

    return (rmi.eax == 0x004f);
}

static int16
vbeSetMode(int16 mode)
{
    rminfo rmi;

    memset(&rmi, 0, sizeof(rmi));
    rmi.eax = 0x4f02;
    rmi.ebx = mode;

    int386rm(0x10, &rmi);

    return (rmi.eax == 0x004f);
}

void CloseMode(void)
{
    vbeSetMode(3);
}


int16
vbeSetBank(uint16 addr)
{
    rminfo rmi;

    memset(&rmi, 0, sizeof(rmi));
    rmi.eax = 0x4f05;
    rmi.ebx = 0x0000;
    rmi.edx = addr;

    int386rm(0x10, &rmi);

    return (rmi.eax == 0x004f);
}


//---------------------------------------------------
//
// Color palette addresses
//

#define PAL_WRITE_ADDR (0x3c8)      // palette write address
#define PAL_READ_ADDR  (0x3c7)      // palette write address
#define PAL_DATA       (0x3c9)      // palette data register

//---------------------------------------------------
//
// Set a range of entries in the color table
//

void
vgaSetPalette(int16 start, int16 count, vgaColor *p)
{
    int16 i;

    if (start < 0 || (start + count - 1) > 255)
    {
        return;
    }

    while(!(inp(0x3da) & 0x08));    // wait vertical retrace

    outp(PAL_WRITE_ADDR, start);
    for (i = 0; i < count; i++)
    {
        outp(PAL_DATA, p->red);
        outp(PAL_DATA, p->green);
        outp(PAL_DATA, p->blue);
        p++;
    }
}


int32
InitMode(void)
{
    if (!vbeGetInfo(&info))
    {
        return 1;
    }
    if (info.vesa[0] != 'V' ||
        info.vesa[1] != 'E' ||
        info.vesa[2] != 'S' ||
        info.vesa[3] != 'A')
    {
        return 2;
    }
    if (info.majorMode == 1 &&
        info.minorMode < 2)
    {
        return 3;
    }
    if (!vbeGetModeInfo(MODENUM, &modeInfo) ||
        !(modeInfo.modeAttr & 1))
    {
        return 4;
    }
    currentBank = -1;
    vbeSetMode(MODENUM);    // set the new mode
    return 0;
}

#define sgn(x) ((x<0)?-1:((x>0)?1:0)) /* macro to return the sign of a
                                         number */

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


  plot_pixel(px,py,color,buffer);

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
      plot_pixel(px,py,color,buffer);
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
      plot_pixel(px,py,color,buffer);
    }
  }
}




