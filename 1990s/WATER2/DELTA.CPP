#include "svgadrv.hpp"
#include <conio.h>
#include <i86.h>
#include <stdlib.h>
#include <time.h>
#include <math.h>
#include <ctype.h>
#include <string.h>




#define GRSIZE_A         1000
#define GRSIZE_B         2000
#define PERIODLINECHANGE 2000
#define NUMLINECHANGE       3
#define LineR              63
#define LineG              63
#define LineB              63
#define BackR              16
#define BackG              16
#define BackB              16
 
int GRSPEED=               30;
int BGSPEED=               15;
int NUMLINES=               5;
int SPREAD=                30;
int PERIODPAL=              3;
int DRAINAGE=              20;
int PERIODUPDATE=           1;

char *buffer;
vgaColor ColorTable[256];


#define Randomize()       (srand(time(0)))
#define Random(x)         (  ( rand()*(x) )  /(RAND_MAX+1) )
#define WritePixel(X,Y,C) plot_pixel((X),(Y),(char)(C),buffer)
#define ReadPixel(X,Y)  (*(buffer + ((int)(X)) + (((int)(Y))*WIDTH)))

void VidInit(void);
void VidClose(void);
void rotatepal(void);
void Initialize(void);
void CleanUp(void);
inline int KeyIn(void);
void ReLine(void);
void ClearPart(void);
void ClearBack(void);
void main(void);



void rotatepal(void)
{
    int16 R,G,B;
    int I;
    R=ColorTable[3].red;
    G=ColorTable[3].green;
    B=ColorTable[3].blue;
    for(I=3;I<255;I++)
    {
        ColorTable[I].red=ColorTable[I+1].red;
        ColorTable[I].green=ColorTable[I+1].green;
        ColorTable[I].blue=ColorTable[I+1].blue;
    }
    ColorTable[255].red=R;
    ColorTable[255].green=G;
    ColorTable[255].blue=B;
    vgaSetPalette(0,256,ColorTable);
}

void Initialize(void)
{
    int I;
    InitMode();
    buffer= new char[HEIGHT*WIDTH];
    memset(buffer,0,HEIGHT*WIDTH);
    Randomize();
    ReLine();
    line(0,0,0,MAXY,1,buffer);
    line(0,MAXY,MAXX,MAXY,1,buffer);
    line(MAXX,MAXY,MAXX,0,1,buffer);
    for(I=3;I<256;I++)
    {
        ColorTable[I].red=(sin((I-3+0)/40.2662006021)+1)*31.5001;
        ColorTable[I].green=(sin((I-3+253/3.0)/40.2662006021)+1)*31.5001;
        ColorTable[I].blue=(sin((I-3+253/1.5)/40.2662006021)+1)*31.5001;
    }
    ColorTable[1].red=LineR;
    ColorTable[1].green=LineG;
    ColorTable[1].blue=LineB;
    ColorTable[2].red=BackR;
    ColorTable[2].green=BackG;
    ColorTable[2].blue=BackB;
    
    vgaSetPalette(0,256,ColorTable);
}


void CleanUp(void)
{
    CloseMode();
    delete[] buffer;
}

inline int KeyIn(void)
{
    int temp;
    while(kbhit())getch();
    temp=getch();
    while(kbhit())getch();
    return temp;
}

void ReLine(void)
{
    int I,II,tx1,tx2,ty1,ty2;
    for(I=1;I<MAXX;I++)
        for(II=0;II<MAXY;II++)
            if (ReadPixel(I,II)==1) WritePixel(I,II,0);
    for (I=0;I<NUMLINES;I++)
    {
        tx1=Random(MAXX);
        tx2=Random(MAXX);
        ty1=Random(MAXY)/2 + MAXY/2;
        ty2=Random(MAXY)/2 + MAXY/2;
        line(short(tx1),short(ty1),short(tx2),short(ty2),1,buffer);
        line(short(tx1+1),short(ty1),short(tx2+1),short(ty2),1,buffer);
        line(short(tx1-1),short(ty1),short(tx2-1),short(ty2),1,buffer);
    }
}

void ClearPart(void)
{
    int I,II;
    for(II=MAXY/2;II<MAXY;II++)
        for(I=1;I<MAXX;I++)
            WritePixel(I,II,0);
}

void ClearBack(void)
{
    int I,II;
    for(I=1;I<MAXX;I++)
        for(II=0;II<MAXY;II++)
            if (ReadPixel(I,II)==2) WritePixel(I,II,0);
}

void main(void)
{
    int I,II, color, tmp0,tmp1,tmp2,tmp3,IIp1,showit=1;
    char grcol;int grpos,grsize=0;
    unsigned int general_count=0;
    int dir=1;
    int status=1;
    Initialize();
    KeyIn();
    while(status)
    {

        grsize --;
        general_count++;
/******/for (I=0;I<DRAINAGE;I++) WritePixel(Random(MAXX-2)+1,MAXY-1,0);
/*THIS*/if (!(general_count%PERIODLINECHANGE))for(I=0;I<NUMLINECHANGE;I++)ReLine();
/* IS */if(grsize<1) {grcol=char(Random(252)+3); grpos=Random(MAXX-SPREAD*2+2)+SPREAD+1; grsize=Random(GRSIZE_A)+GRSIZE_B;}
/*GUNK*/for (I=0;I<GRSPEED;I++) WritePixel(grpos+Random(SPREAD*0.5)-Random(SPREAD*0.5)+Random(SPREAD*0.5)-Random(SPREAD*0.5),0,grcol);
/******/for (I=0;I<BGSPEED;I++) WritePixel(Random(MAXX-2)+1,0,2);
        
        for (II=(MAXY-2);II>=0;II--)
        {
            for(I=(MAXX-1);I;I--)
            {
                color = ReadPixel(I,II);
                if(color>1)
                {
                    tmp0=!ReadPixel(I  ,IIp1=II+1);
                    tmp1=!ReadPixel(I+1,IIp1);
                    tmp2=!ReadPixel(I-1,IIp1);
                    
                    if (tmp0)
                    {
                        tmp3=ReadPixel(I-1,II);
                        if(tmp1&&tmp3&&((!tmp2)||(ReadPixel(I-2,II)&&(ReadPixel(I-2,IIp1)||(ReadPixel(I-3,II)&&(ReadPixel(I-3,IIp1)||(ReadPixel(I-4,II)&&(ReadPixel(I-4,IIp1)||(ReadPixel(I-5,II)&&(ReadPixel(I-5,IIp1)||(ReadPixel(I-6,II)&&ReadPixel(I-6,IIp1))))))))))))
                                   {WritePixel(I+1, IIp1, color);  WritePixel(I,II,0);} 
                        else       {WritePixel(I,   IIp1, color);  WritePixel(I,II,0);}
                    }
                    else if (tmp1) {WritePixel(I+1, IIp1, color);  WritePixel(I,II,0);}
                    else if (tmp2) {WritePixel(I-1, IIp1, color);  WritePixel(I,II,0);}
                }
            }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////          
   // Ok, so every second line is mapped mapped the opposite way just like those old OLD CGA's in your cellar, so the //
  // sand heaps stay simetrical (In theory).  This also means that at high sand flow, the gradient is 2 not one, and //
 // if that sounds confusing look at the output, wherever it's steeper than 45 degrees it's because of this.  Yes?? //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            II--;
            for(I=1;I<MAXX;I++)
            {
                color = ReadPixel(I,II);
                if(color>1)
                {
                    tmp0=!ReadPixel(I  ,IIp1=II+1);
                    tmp1=!ReadPixel(I+1,IIp1);
                    tmp2=!ReadPixel(I-1,IIp1);
                    
                    if (tmp0)
                    {
                        tmp3=ReadPixel(I+1,IIp1);
                        if(tmp2&&tmp3&&((!tmp1)||(ReadPixel(I+2,II)&&(ReadPixel(I+2,IIp1)||(ReadPixel(I+3,II)&&(ReadPixel(I+3,IIp1)||(ReadPixel(I+4,II)&&(ReadPixel(I+4,IIp1)||(ReadPixel(I+5,II)&&(ReadPixel(I+5,IIp1)||(ReadPixel(I+6,II)&&ReadPixel(I+6,IIp1))))))))))))
                                   {WritePixel(I-1, IIp1, color);  WritePixel(I,II,0);} 
                        else       {WritePixel(I,   IIp1, color);  WritePixel(I,II,0);}
                    }
                    else if (tmp1) {WritePixel(I+1, IIp1, color);  WritePixel(I,II,0);}
                    else if (tmp2) {WritePixel(I-1, IIp1, color);  WritePixel(I,II,0);}
                }
            }
        }
        if(showit)
        {
            if(!(general_count%PERIODUPDATE))
                UpdateScr(buffer);
            if(!(general_count%PERIODPAL))
                rotatepal();
        }
        
        while(kbhit())
        {
            tmp1=toupper(getch());
            switch (tmp1)
            {
                case  27: status=0;                                         break;//ESC exits
                case '1': ReLine();                                         break;//1 draws new lines
                case '2': ClearPart();                                      break;//2 clears the bottom half of the screen
                case '3': ClearBack();                                      break;//3 clear all "background pixels
                case '4': grsize=0;                                         break;//4 changes foreground group
                case 'A': GRSPEED++;                                        break;//A/Z adjusts foreground flow amount
                case 'Z': GRSPEED--;                                        break;
                case 'S': BGSPEED++;                                        break;//S/X adjusts background flow amount
                case 'X': BGSPEED--;                                        break;
                case 'D': DRAINAGE++;                                       break;// D/C adjusts Leaking from Screen bottom
                case 'C': DRAINAGE--;                                       break;
                case 'F': SPREAD++;                                         break;// F/V adjusts spread of forground group
                case 'V': SPREAD--;                                         break;
                case 'B': PERIODPAL++;                                      break;//G/B adjusts speed of palette animation
                case 'G': PERIODPAL--; if(!PERIODPAL)PERIODPAL=1;           break;
                case 'H': NUMLINES++; ReLine();                             break;//H/N adjusts number of lines
                case 'N': NUMLINES--; ReLine();                             break;
                case 'P': while(kbhit()) getch(); getch();                  break;//P pauses
                case '+': PERIODUPDATE++;                                   break;//+/- adjust flow speed at cost of
                case '-': PERIODUPDATE--; if(!PERIODUPDATE)PERIODUPDATE=1;  break;//    refresh rate
                case '5': if(!showit)
                          {
                              showit=1;
                              InitMode();
                              ColorTable[1].red=LineR;
                              ColorTable[1].green=LineG;
                              ColorTable[1].blue=LineB;
                              ColorTable[2].red=BackR;
                              ColorTable[2].green=BackG;
                              ColorTable[2].blue=BackB;
                              vgaSetPalette(0,3,ColorTable);
                          }                                                 break;//5 switches to graphics mode
                case '6': if(showit) {showit=0;CloseMode();}                break;//6 switches to text mode so that
            }                                                                     //  "water2" can run in the background
        }                                                                         //  of WIN95
    }
    CleanUp();
}

