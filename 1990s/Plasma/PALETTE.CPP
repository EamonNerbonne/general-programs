#include <dos.h>
#include <conio.h>
#include <stdio.h>
#include <stdlib.h>

#define VGAPTR (byte *)0xA0000000L
#define pp(x,y,color) VGA[(y<<6)+(y<<8)+x]=color;

typedef unsigned char byte;
typedef struct {int r,g,b;} Palette;
byte *VGA=VGAPTR;

int gp(int,int);

void SetPalette(Palette *p)
{
		int i;

		while(!(inp(0x3da) & 0x08));    // wait vertical retrace
		 outp(0x3c8, 0);
		for (i=0;i<256;i++)
		{
		 outp(0x3c9, p->r);
		 outp(0x3c9, p->g);
		 outp(0x3c9, p->b);
		 p++;
		}
}

void main(void)
{
	unsigned int x,y,k,px,py,c;
	union REGS regs;
	Palette palette[256];   //NOTICE CASE!! Palette's a typedef, palette just a variable.
	for (x=0;x<256;x++)       //Make red gradient
	{
		palette[x].r=x>>2; // devide by 4 - because max is 64, not 256
		palette[x].g=0;    // (note that you get vertical similarly
		palette[x].b=0;    // colored bars 4 pizels wide. you have 64^3
	}                  // colors, not 256^3 :)

	regs.h.ah=0x00; regs.h.al=0x13; int86(0x10, &regs, &regs);


	SetPalette(palette);
/*	for (y=189;y>10;y--)
		for (x=10;x<310;x++)
			pp(x,y,x+rand()%50);
*/
	for (x=0;x<64000;x++)
		VGA[(y<<8)+(y<<6)+x] = 100;

	while(!kbhit())
	{
/*		for (y=0;y<200;y++)
			for (x=0;x<320;x++)
				pp(x,y,(gp(x-1,y-1) +
								gp(x-1,y  ) +
								gp(x-1,y+1) +
								gp(x  ,y-1) +
								gp(x  ,x+1) +
								gp(x+1,y-1) +
								gp(x+1,y  ) +
								gp(x+1,y+1)) >> 3);
*/

	c=0;
	px = rand()%300+10;
	py = rand()%180+10;
	pp(px-1,py-1,c);
	pp(px-1,py  ,c);
	pp(px-1,py+1,c);
	pp(px  ,py-1,c);
	pp(px  ,px+1,c);
	pp(px+1,py-1,c);
	pp(px+1,py  ,c);
	pp(px+1,py+1,c);

	for (x=0;x<64000;x++)
		if (VGA[(y<<8)+(y<<6)+x] != 0&&VGA[(y<<8)+(y<<6)+x] != 100)
		VGA[(y<<8)+(y<<6)+x]=(VGA[(y<<8)+(y<<6)+x-1-320] +
													VGA[(y<<8)+(y<<6)+x-1    ] +
													VGA[(y<<8)+(y<<6)+x-1+320] +
													VGA[(y<<8)+(y<<6)+x  -320] +
													VGA[(y<<8)+(y<<6)+x  +320] +
													VGA[(y<<8)+(y<<6)+x+1-320] +
													VGA[(y<<8)+(y<<6)+x+1    ] +
													VGA[(y<<8)+(y<<6)+x+1+320]) >> 3;

	}
	getch();
}

/******************************************************************
															getpixel
 ******************************************************************/

int gp(int x, int y)
{
	return VGA[(y<<8)+(y<<6)+x];
}
