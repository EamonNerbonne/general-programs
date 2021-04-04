//#include <graphics.h>
#include <conio.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <allegro.h>

#define length 20
#define you_to_screen 1000
#define distance 100
#define magnification 20
#define MAXX 640
#define MAXY 480

double z_value[length][length]; /*function z = f(x, y)*/
int col;
BITMAP *BUF;
struct
{
  double x;
  double y;
  double z;
} point_array[length][length];

float speed_around_x, speed_around_y, speed_around_z;




Draw3D()
{
  float screen_to_z;
  int z, i, j, x, y, prevx, prevy, nextx, nexty, idir, jdir, iddd, jddd;

  prevx = 0;
  prevy = 0;
  col = 15;

/*  if (point_array[i]/**/
  if (point_array[0][0].z < point_array[length - 1][0].z) idir = 1; else idir = -1;
  if (point_array[0][0].z < point_array[0][length - 1].z) jdir = 1; else jdir = -1;
  iddd =  (idir==1 ? 0 : length - 1);
  jddd =  (jdir==1 ? 0 : length - 1);
  for (i = iddd; (i < length - idir) && (i > 0 - idir); i+=idir)
//  for (i = 0; i <= length - 1; i++)
  {
    screen_to_z = distance - point_array[i][jddd].z;
    prevx = you_to_screen * point_array[i][jddd].x * magnification / (you_to_screen + screen_to_z);
    prevy = you_to_screen * point_array[i][jddd].y * magnification / (you_to_screen + screen_to_z);

    for (j = (jdir==1 ? 0 : length-1); (j <= length - 1) && (j >= 0); j+=jdir)
//    for (j = 0; j < length; j++)
    {
      if (point_array[i][j].z > distance) {printf ("AAAAAAAAAAAAAAAAAAA!!!"); getch(); exit(1);}
      screen_to_z = distance - point_array[i][j].z;
      x = you_to_screen * point_array[i][j].x * magnification / (you_to_screen + screen_to_z);
      y = you_to_screen * point_array[i][j].y * magnification / (you_to_screen + screen_to_z);
/*      putpixel (300 + x, 200 + y, 15);*/
      z = (int)point_array[i][j].z * 2.5;
      col = z + 30;
      line (BUF, MAXX/2 + prevx, MAXY/2 + prevy, MAXX/2 + x, MAXY/2 + y, col);

      screen_to_z = distance - point_array[i + idir][j].z;
      nextx = you_to_screen * point_array[i + idir][j].x * magnification / (you_to_screen + screen_to_z);
      nexty = you_to_screen * point_array[i + idir][j].y * magnification / (you_to_screen + screen_to_z);

      line (BUF, MAXX/2 + x, MAXY/2 + y, MAXX/2 + nextx, MAXY/2 + nexty, col);
/**/
      prevx = x;
      prevy = y;
/*      getch();/**/
    }
  }
}






turn_around_y (float degrees)
{
  char i, j;
  float a, b;
  degrees = degrees * 2 * 3.1416 / 360;
  for (i = 0; i < length; i++)
    for (j = 0; j < length; j++)
    {
      a = point_array[i][j].x * cos(degrees) + point_array[i][j].z * sin(degrees);
      b = -(point_array[i][j].x * sin(degrees)) + point_array[i][j].z * cos(degrees);/**/

      point_array[i][j].x = a;
      point_array[i][j].z = b;
    }
}

turn_around_x (float degrees)
{
  char i, j;
  float a, b;
  degrees = degrees * 2 * 3.1416 / 360;
  for (i = 0; i < length; i++)
    for (j = 0; j < length; j++)
    {
      a = point_array[i][j].y * cos(degrees) + point_array[i][j].z * sin(degrees);
      b = -(point_array[i][j].y * sin(degrees)) + point_array[i][j].z * cos(degrees);/**/

      point_array[i][j].y = a;
      point_array[i][j].z = b;
    }
}

turn_around_z (float degrees)
{
  char i, j;
  float a, b;
  degrees = degrees * 2 * 3.1416 / 360;
  for (i = 0; i < length; i++)
    for (j = 0; j < length; j++)
    {
      a = point_array[i][j].x * cos(degrees) + point_array[i][j].y * sin(degrees);
      b = -(point_array[i][j].x * sin(degrees)) + point_array[i][j].y * cos(degrees);/**/

      point_array[i][j].x = a;
      point_array[i][j].y = b;
    }
}











	/*----------------------------------------------------------*/




main()
{
  long i, j, x, y, n, total;
  char keyread;
  PALETTE my_palette;
/*  gdriver = installuserdriver ("vesa", 0);
  gmode = 3;
  initgraph(&gdriver, &gmode, "");*/
  allegro_init();
  set_gfx_mode(GFX_VESA1, MAXX, MAXY, 0, 0);
  BUF = create_bitmap (MAXX, MAXY);
  clear(screen);

  for (i = 0; i < 64; i++)
  {
    my_palette[i].r = i;
    my_palette[i].g = i;
    my_palette[i].b = i;
  }
  for (i = 64; i < 256; i++)
  {
    my_palette[i].r = 0;
    my_palette[i].g = 0;
    my_palette[i].b = 0;
  }
  set_palette(my_palette);


//  setlinestyle(0,0,0);

  for (i = -(length/2); i < (length/2); i++)
    for (j = -(length/2); j < (length/2); j++)
      z_value[i + (length/2)][j + (length/2)] =
//  5*(   sin(1*i)*cos(2*j)   ) / ((i*i + 0.2) + (j*j + 0.2));
   10*(   exp(-0.02*(i)*(i))*exp(-0.02*(j)*(j))    );
//   fabs(5*(sin(i*0.4)));
//   0.08 *(  i*i - j*j);
//   6*sin(0.06*(i*i+j*j))/(0.06*(i*i+j*j)+0.1);
//   -sqrt(fabs(i)*fabs(j));
//    2*cos(0.5*sqrt(i*i + j*j));
//     0.5/(0.05*i*i + 0.2) * 1 /(0.05*j*j + 0.2);


/*   for (i = 0; i < 20; i++)
    for (j = 0; j < 20; j++)
      putpixel(50 + i, 50 + j, (z_value[i][j] / 2) + 16);/**/


  for (i = 0; i < length; i++)
    for (j = 0; j < length; j++)
    {
      point_array[i][j].x = i - (length/2);
      point_array[i][j].y = j - (length/2);
      point_array[i][j].z = z_value[i][j];
    }

/*  keyread = 36;*/
  while (kbhit()) getch();

  while (!(keyread == 27))
  {
/*    setfillstyle (1, 0);
    bar (0, 0, 640, 480);*/
    if (kbhit())
    {
      keyread = getkey();
      switch (keyread)
      {
        case 77/*right*/: speed_around_y+=0.3; break;
        case 75/*left*/: speed_around_y-=0.3; break;
        case 72/*up*/: speed_around_x-=0.3; break;
        case 80/*down*/: speed_around_x+=0.3; break;
        case 71/*up left*/: speed_around_z+=0.3; break;
        case 73/*up right*/: speed_around_z-=0.3; break;
        case ' ': speed_around_x = speed_around_y = speed_around_z = 0; break;
        case 27: exit(0);
      }
      keyread = 36;
    }
    turn_around_y (speed_around_y/*degrees*/);
    turn_around_x (speed_around_x/*degrees*/);
    turn_around_z (speed_around_z/*degrees*/);

    clear(BUF);
    Draw3D();
    blit(BUF, screen, 0, 0, 0, 0, MAXX, MAXY);


  }/*while*/

  return 0;
}