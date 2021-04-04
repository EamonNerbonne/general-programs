#include <math.h>
#include <conio.h>
#include <stdio.h>
#include <iostream.h>
#include <time.h>
#include <stdlib.h>

#include <svgadrv.hpp>

#define PX1 0.0082345
#define PY1 0.027268
#define PX2 0.0103435
#define PY2 0.0134
#define SPD_INC 1.04

#define sqr(a) ((a)*(a))

double dist(double x, double y, double x1, double y1);
void draw_circs(char * circles, double C_SIZE);
void draw_spastic_circs(char * circles, double C_SIZE, int freq,double amp);
void make_palette(void);
void addbmp(char*bmp1,char*bmp2,char*target,
           int x1,int y1,
           int x2,int y2,
           int xt,int yt,
           int xres,int yres);
char*screen;

void main(int argc,char*argv[])
{
  char *circles1, *circles2;
  int quit_it=0;
  int counter=0,start,end;
  char input;
  int _1freq,_2freq;
  double _1size,_2size,_1amp,_2amp;
  double cpu_slow=1;
  double period_x1 = 0,
         period_y1 = 0,
         period_x2 = 0,
         period_y2 = 0;
  FILE*inifile;
  if (argc!=2) {printf("You need one input, the initialization file"); exit(1);}
  screen=InitMode();
  inifile=fopen(argv[1],"rt");
  circles1 = new char [WIDTH*2*HEIGHT*2];
  circles2 = new char [WIDTH*2*HEIGHT*2];
  make_palette();
  fscanf(inifile," %lf %d %lf %lf %d %lf ",&_1size,&_1freq,&_1amp,&_2size,&_2freq,&_2amp);
  draw_spastic_circs(circles1, _1size,_1freq,_1amp);
  draw_spastic_circs(circles2, _2size,_2freq,_2amp);
  start=clock();
  while(!quit_it)
  {
    counter++;
    period_x1 += PX1*cpu_slow;
    period_y1 += PY1*cpu_slow;
    period_x2 += PX2*cpu_slow;
    period_y2 += PY2*cpu_slow;

    addbmp(circles1,circles2,screen, WIDTH/2*(1 + sin(period_x1)), HEIGHT/2*(1 + sin(period_y1)),
                                   WIDTH/2*(1 + sin(period_x2)), HEIGHT/2*(1 + sin(period_y2)),
                                                        0, 0,
                                                    WIDTH, HEIGHT);
    while (kbhit())
    {
        input=(char)getch();
        if((input=='+')||(input=='=')) cpu_slow*=SPD_INC;
        if(input=='-') cpu_slow/=SPD_INC;
        if(input==' ') cpu_slow=1;
        if(input==27) quit_it=1;
    }
  }
  end=clock();
  CloseMode();
  cout<<"FPS: "<<counter/((end-start)/double(CLOCKS_PER_SEC))<<endl;
  cout<<"Have a nice day.\n";
}






double dist(double x, double y, double x1, double y1)
{
return sqrt(sqr(x-x1)+sqr(y-y1));
}

void draw_circs(char* circles, double C_SIZE)
{
  int x,y;
  int c;
  for(y=0;y<HEIGHT*2;y++)
    for(x=0;x<WIDTH*2;x++)
    {
      c=(char)(64 + 64*cos(C_SIZE*dist(x,y,WIDTH,HEIGHT)));
      *(circles+y*WIDTH*2+x)=char(c);
      PutPixel(x/2,y/2,c,screen);
    }
}


void draw_spastic_circs(char * circles, double C_SIZE, int freq,double amp)
{
  int x,y;
  double d;
  int c;

  for(y=0;y<HEIGHT*2;y++)
    for(x=0;x<WIDTH*2;x++)
    {
      d=dist(x,y,WIDTH,HEIGHT);
      c= (char)(64 + 64*cos(C_SIZE*d*(amp+cos(freq*acos((x-WIDTH)/((int)(100*d)*0.01+0.005))))));
      *(circles+y*WIDTH*2+x)=char(c);
      PutPixel(x/2,y/2,c,screen);
    }
}


void make_palette(void)
{
  ColorST pal[256];
  int i;
  for(i=0;i<64;i++)
  {
    pal[i].r=63-i;
    pal[i].g=(63-i)/2+32;
    pal[i].b=63-i;
  }
  for(i=0;i<64;i++)
  {
    pal[i+64].r=0;
    pal[i+64].g=(63-i)/2;
    pal[i+64].b=0;
  }
  for(i=0;i<64;i++)
  {
    pal[i+128].r=0;
    pal[i+128].g=0;
    pal[i+128].b=i/2;
  }
  for(i=0;i<64;i++)
  {
    pal[i+192].r=i/2;
    pal[i+192].g=0;
    pal[i+192].b=i/2+32;
  }
  SetPalette(pal);
}
void addbmp(char*bmp1,char*bmp2,char*target,
           int x1,int y1,
           int x2,int y2,
           int xt,int yt,
           int xres,int yres)
{
  int x,y;

  int*p1,*p2,*pt;
  x1/=4;
  x2/=4;
  xt/=4;
  xres/=4;

  for(y=0;y<yres;y++)
  {
    pt=(int*)(target+(WIDTH*(yt+y)));
    p1=(int*)(bmp1+(WIDTH*2*(y1+y)));
    p2=(int*)(bmp2+(WIDTH*2*(y2+y)));
    for(x=0;x<xres;x++)
    {
      pt[xt+x]=p1[x1+x]+p2[x2+x];
    }
  }
}


