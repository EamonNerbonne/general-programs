#include "grph_13h.cpp"
#include <math.h>
#define SH 5
#define CP 6
#define SP 1
#define LN 10
#define RN 90
#define MX 100
#define PN 50
#define OFF 1.0000000000000001

void main(void)
{
    char *buffer1,*buffer2;
    int Xt[PN],Yt[PN],I,X,Y,loopvar,tmp;
    ColorST ColorTable[256];
    SetMode();
    buffer1=new char[320*200];
    buffer2=new char[320*200];
    randomize();
    for(I=0;I<PN;I++)
        {Xt[I]=random(320);Yt[I]=random(200);}
    for(Y=0;Y<200;Y++)
        for(X=0;X<320;X++)
            PutPixel(X,Y,random(RN)+1,buffer1);
    ColorTable[0].r=0;
    ColorTable[0].g=0;
    ColorTable[0].b=0;
    for(I=1;I<64;I++)
    {
        ColorTable[I].r=(64-I);
        ColorTable[I].g=(64-I);
        ColorTable[I].b=(64-I);
    }
    for(I=64;I<96;I++)
    {
        ColorTable[I].r=(I-64);
        ColorTable[I].g=0;
        ColorTable[I].b=0;
    }
    for(I=96;I<256;I++)
    {
        ColorTable[I].r=32+((I-96)/5);
        ColorTable[I].g=(I-96)/20;
        ColorTable[I].b=(I-96)/32;
    }
    SetPalette(ColorTable);
    UpdateScr(buffer1,SCREEN);
    getch();
    for(loopvar=0;loopvar<LN;loopvar++)
    {
        for(I=0;I<320;I++)
        {
            PutPixel(I,0,1,buffer1);
            PutPixel(I,199,1,buffer1);
        }
        for(I=1;I<199;I++)
        {
            PutPixel(0,I,1,buffer1);
            PutPixel(319,I,1,buffer1);
        }
        for(I=0;I<PN/2;I++)
            PutPixel(Xt[I],Yt[I],1,buffer1);
        for(I=PN/2;I<PN;I++)
            PutPixel(Xt[I],Yt[I],MX,buffer1);
        for(Y=0;Y<200;Y++)
            for(X=0;X<320;X++)
                PutPixel(X,Y,
                             (GetPixel(X-1,Y-1,buffer1)+
                              GetPixel(X  ,Y-1,buffer1)+
                              GetPixel(X+1,Y-1,buffer1)+
                              GetPixel(X-1,Y  ,buffer1)+
                              GetPixel(X+1,Y  ,buffer1)+
                              GetPixel(X-1,Y+1,buffer1)+
                              GetPixel(X  ,Y+1,buffer1)+
                              GetPixel(X+1,Y+1,buffer1))>>3
                                                           ,buffer2);
        UpdateScr(buffer2,buffer1);
        UpdateScr(buffer1,SCREEN);
    }
    for(I=0;I<320;I++)
    {
        PutPixel(I,0,255,buffer1);
        PutPixel(I,199,1,buffer1);
    }
    for(I=1;I<199;I++)
        {
        PutPixel(0,I,1,buffer1);
        PutPixel(319,I,1,buffer1);
    }
    /*
    {
        double *Dscr;
        Dscr=new double[320*200];
        #define putD(x,y,c) (*(Dscr+(x)+320*(y))=double(c))
        #define getD(x,y) (*(Dscr+(x)+320*(y))+0)
        double sum,max,min;
        for(Y=0;Y<200;Y++)
            for(X=0;X<320;X++)
            {
                sum=0;
                for(I=0;I<PN;I++)
                {
                    tmp=1/((Xt[I]-X)*(Xt[I]-X)+(Yt[I]-Y)*(Yt[I]-Y)+OFF);
                    sum+=tmp;
                }
                putD(X,Y,sum);
            }
        max=min=*Dscr;
        for(Y=0;Y<200;Y++)
            for(X=0;X<320;X++)
                if (getD(X,Y)<min) min=getD(X,Y); else if (getD(X,Y)>max) max=getD(X,Y);
        sum=max-min;
        sum=(MX-1.00001)/(max-min);
        for(Y=0;Y<200;Y++)
            for(X=0;X<320;X++)
                PutPixel(X,Y,MX-(getD(X,Y)-min)*sum,buffer1);
        delete[] Dscr;
        for(I=0;I<320;I++)
            PutPixel(I,0,255,buffer1);
    }*/

    
    while(!kbhit())
    {
        for(Y=0;Y<200;Y++)
            for(X=0;X<320;X++)
            {
                tmp=((((int) (GetPixel(X-1,Y-1,buffer1)>>SH)+
                       (int) GetPixel(X  ,Y-1,buffer1)+
                       (int) (GetPixel(X+1,Y-1,buffer1)>>SH))>>CP)<<SP)+
                    GetPixel(X,Y,buffer1);
                PutPixel(X,Y,(tmp>255)?255:tmp,buffer2);
            }
        UpdateScr(buffer2,buffer1);
        UpdateScr(buffer1,SCREEN);



    }
    
    getch();
    TextMode();
    delete[] buffer1;
    delete[] buffer2;
    
}
