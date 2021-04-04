//#include <graphics.h>
#include <conio.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <allegro.h>

#define graphmode 0

#define length 10
#define height 10
#define you_to_screen 20
#define magnification 14
#define yourheight 2.5

#define MAXX 640
#define MAXY 480

struct {float x; float y; float z; float bearing;} you;
int col;
BITMAP *BUF;


Draw3D (float x1, float y1, float z1, float x2, float y2, float z2)
{
   float newx, newy, XX1, YY1, XX2, YY2, screen_to_y;

   x1 += you.x;
   y1 += you.y;
   newx = x1 * cos(you.bearing) + y1 * sin(you.bearing);
   newy = -(x1 * sin(you.bearing)) + y1 * cos(you.bearing);/**/
   screen_to_y = newy * magnification - you_to_screen;
   if (screen_to_y < -magnification -5 ) return(1);/**/
   YY1 = you_to_screen * (z1 - you.z) * magnification / newy;
   XX1 = you_to_screen * newx * magnification / newy;
/*   if (dir < 0) YY1 = getmaxy() - YY1;/**/

   x2 += you.x;
   y2 += you.y;
   newx = x2 * cos(you.bearing) + y2 * sin(you.bearing);
   newy = -(x2 * sin(you.bearing)) + y2 * cos(you.bearing);/**/
   screen_to_y = newy * magnification - you_to_screen;
   if (screen_to_y < -magnification - 5) return(1);/**/
   YY2 = you_to_screen * (z2 - you.z) * magnification / newy;
   XX2 = you_to_screen * newx * magnification / newy;
/*   if (dir < 0) YY2 = getmaxy() - YY2;/**/
   line (BUF, (MAXX / 2) + XX1, (MAXY / 2) - YY1, (MAXX / 2) + XX2, (MAXY / 2) - YY2, col);
}





DrawFloor()
{
  int i, j;

  col = 15;
  for (i = 0; i < length - 1; i++)
    for (j = 0; j < length; j++)
    {
      Draw3D (i - (length/2), j - (length/2), 0, i - (length/2) + 1, j - (length/2), 0);
      Draw3D (i - (length/2), j - (length/2), 0, i - (length/2), j - (length/2) + 1, 0);
    }
}




DrawCeiling()
{
  int i, j;

  col = 6;
  for (i = 0; i < length - 1; i++)
    for (j = 0; j < length; j++)
    {
      Draw3D (i - (length/2), j - (length/2), height, i - (length/2) + 1, j - (length/2), height);
      Draw3D (i - (length/2), j - (length/2), height, i - (length/2), j - (length/2) + 1, height);
    }
}

DrawWall()
{
  int i, h;

  col = 4;
  for (i = 0; i < length - 1; i++)
    for (h = 0; h < height; h++/*=(height / length)*/)
    {
      Draw3D (i - (length/2), (length/2), h, i - (length/2) + 1, (length/2), h    );
      Draw3D (i - (length/2), (length/2), h, i - (length/2)    , (length/2), h + 1);/**/

      Draw3D (i - (length/2), -(length/2)-h, h, i - (length/2) + 1, -(length/2)-h, h    );
      Draw3D (i - (length/2), -(length/2)-h, h, i - (length/2)    , -(length/2)-h-1, h + 1);/**/

    }
}





MoveForward (float step)
{
  you.x += step * sin(you.bearing);
  you.y -= step * cos(you.bearing);
}

MoveSide (float step)
{
  you.x -= step * cos(you.bearing);
  you.y -= step * sin(you.bearing);
}



	/*--------------------------------------------------*/



main()
{
  int gdriver, gmode;
  char keyread;
/*  gdriver = installuserdriver ("vesa", 0);
  gmode = graphmode;
  initgraph(&gdriver, &gmode, "");*/
  allegro_init();
  set_gfx_mode(GFX_VESA1, MAXX, MAXY, 0, 0);
  BUF = create_bitmap (MAXX, MAXY);
  clear(screen);
  install_keyboard();

  you.x = you.y = you.bearing = 0;
  you.z = yourheight; /*vertical*/


  while (kbhit()) getch();
  keyread = 36;
  while (!key[1])
  {
/*    setfillstyle (1, 0);
    bar (0, 0, 640, 480); /**/

    clear(BUF);
    DrawFloor();
    DrawCeiling();
    DrawWall();
    blit(BUF, screen, 0, 0, 0, 0, MAXX, MAXY);

/*    keyread = getch();
    switch (keyread)
    {
      case 0:
	keyread = getch();
	switch (keyread)
	{
	  case 77: you.bearing -= 0.1; break;
	  case 75: you.bearing += 0.1; break;
	  case 72: MoveForward (0.3); break;
	  case 80: MoveForward (-0.3); break;
	}; break;
      case '-': you.z += 0.5; break;
      case '+': you.z -= 0.5; break;
      case '/': MoveForward(3); break;
      case 'b': MoveSide (-1); break;
      case 'n': MoveSide (1); break;

    }*/
    if (key[77]) you.bearing -= 0.02;
    if (key[75]) you.bearing += 0.02;
    if (key[72]) MoveForward (0.2);
    if (key[80]) MoveForward (-0.2);
    if (key[74]) you.z += 0.2;
    if (key[78]) you.z -= 0.2;
//    if (key[49]  case '/': MoveForward(3); break;
    if (key[48]) MoveSide (-0.3);
    if (key[49])
    MoveSide (0.3);


  }
  return 0;
}