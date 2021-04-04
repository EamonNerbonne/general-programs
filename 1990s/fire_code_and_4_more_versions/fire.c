/*  Flamey thing:) Was my attempt to nicely use palettes.. Even though
	 I'm pulling some confusing case cheats, i get palettes now (actually
    they're just not so logical (r/g/b range 0-63!?!?) and hardly
    explained nicely... But anyways.

    The flame colors should be easy to chenge. I'll make a better version,
    a plant/harvest colors thing.. Ermm you'll see if you get the version.

    As I understand, borland's dos/i86 interfacing header is <dos.h> but
    watcom's is <i86.h> - so adjust it to yer needs..

    -Bart (scarfman@geocities.com)
*/

#include <math.h>
#include <mem.h>
#include <stdio.h>
#include <stdlib.h>
#include <dos.h>
#include <conio.h>

typedef unsigned char byte;
typedef struct {int r,g,b;} Palette;
byte *VGA=(byte *)0xA0000000L;

void SetPalette(Palette *p)
{   int i;
    while(!(inp(0x3da) & 0x08));    // wait vertical retrace
    outp(0x3c8, 0);
    for (i=0;i<256;i++)
    { outp(0x3c9, p->r); outp(0x3c9, p->g); outp(0x3c9, p->b); p++; }
}

void main (void)
{
 unsigned int x,t;
 unsigned long l;
 union REGS regs;
 float r,g,b, dr,dg,db;
 Palette palette[256], tempalette[256];

 regs.h.ah=0x00; regs.h.al=0x13; int86(0x10, &regs, &regs);

 //first: defince colors--i.e. define palette.
 // ..oo[blue]oo[yellow]oo[red]OOOOO[orangeish]ooooooo........[black]

 for (x=0;x<256;x++)       //Clear palette.
 {palette[x].r=0;
  palette[x].g=0;
  palette[x].b=0;}

 r=0;  g=0;  b=0;

 dr=10/5.0; dg=7/5.0; db=230/5.0;
 for (x=0;x<=5;x++)              //read these like: (0,0,0) - (10,7,230) in 5 steps
 {palette[256-x].r=(int)r>>2;
  palette[256-x].g=(int)g>>2;
  palette[256-x].b=(int)b>>2;
  r+=dr; g+=dg; b+=db;}
  r=10;g=7;b=230;

 dr=175/20.0; dg=185/20.0; db=-230/20.0;
 for (x=6;x<=25;x++)   
 {palette[256-x].r=(int)r>>2;               
  palette[256-x].g=(int)g>>2;
  palette[256-x].b=(int)b>>2;
  r+=dr; g+=dg; b+=db;}
  r=195;g=195;b=0;

 dr=45/20.0; dg=-100/20.0; db=0;
 for (x=26;x<=45;x++)     
 {palette[256-x].r=(int)r>>2;     
  palette[256-x].g=(int)g>>2;
  palette[256-x].b=(int)b>>2;
  r+=dr; g+=dg; b+=db;}
  r=240;g=96;b=0;

 dr=0; dg=-96/5.0; db=0;
 for (x=46;x<=50;x++)      
 {palette[256-x].r=(int)r>>2;      
  palette[256-x].g=(int)g>>2;
  palette[256-x].b=(int)b>>2;
  r+=dr; g+=dg; b+=db;}
  r=240;g=32;b=0;

 dr=-240/10.0; dg=0; db=0;
 for (x=51;x<=60;x++)    
 {palette[256-x].r=(int)r>>2;        
  palette[256-x].g=(int)0>>2;
  palette[256-x].b=(int)b>>2;
  r+=dr; g+=dg; b+=db;}
  /*the reason for [256-x] is because the fading effect I use will
    decrease the overall value.. Which should mean down from blue,
    not up as it should be.. I just prefer thinking of the palette 
    the other way around and be done with it:)

    I could have bothered using a rgb scale from 0 to 63.. But I'm
    too used to it this way (like in html u know...)
  */


   //This last because it streatched up too far even though I only used the
   //first 90 colors on the palette.. Now 45.
 SetPalette(palette);


/*for (x=0;x<200;x++)          //show gradient. Only reason I declare
  for (l=0;l<255;l++)          //x and l seperately... 'cos i otherwise only
   VGA[l+(x<<6)+(x<<8)]=l%256; //use one at a time.
  getch();
*/

 for (l=0;l<64000L;l++) VGA[l]=0; //clear screen;


 while(!kbhit())
 {              //62400-64000
  for (l=63680L;l<64000L;l++)
   VGA[l]=256-rand()%10;

  for (l=63680L;l>42000;l--) //i cheat.. It's never gonna get upto there anyways..
   VGA[l]=((VGA[l]+
   			VGA[l+319]+
            VGA[l+320]+
            VGA[l+321]  )>>2)+(l<<8);
 												//I know, a double bufferish thing
                                    //would be nice.. I'm waiting for
                                    //dos4g/w though so i can use
                                    //memory without bother...
 }
//                regs.h.al=0x03; int86(0x10, &regs, &regs);
}

