#include <math.h>
#include <STDLIB.H>
#include <CONIO.H>
#include <STDIO.H>
#include <allegro.h>
/*#define Wait 5000
#define BallSize 10
#define Friction 350
#define ExtraFr 0.3
#define Acceleration 0.0006
#define Bounce 0.5*/
//#define printthings

#define normal

#define Wait1 80000
#define Wait2 100000
#define LivesNum 3
#define BallSize 10

#define Friction 350
#define ExtraFr 0.3
#define Acceleration 0.0008
#define Bounce 0.5

#define FillColour 3
#define LineColour 11
#define TotalSpeed 0.283
//#define TotalSpeed 0.183

#define MAXX 800
#define MAXY 600

#define PutPixel(x,y,c) _putpixel(screen,x,y,c)
#define PutRealPixel(x,y,c) _putpixel(RealScreen,x,y,c)
#define GetPixel(x,y) _getpixel(screen,x,y)
#define GetRealPixel(x,y) _getpixel(RealScreen,x,y)
#define PutUnderImage(x,y,a,b) blit(RealScreen,screen,x,y,x,y,a,b)
#define Random(x) ((unsigned int)(random()*((float)x)/(2147483648.)))

#define equals(x, y, z)  (((__a=x) == y) || (__a == z))

struct
  {float X, Y, XSpeed, YSpeed;}
    ball[10];

struct
  {int X, Y;}
    YourLine[6000];

int YourPixel;

int __a;

int I, sign = 1, BallNumber, Level, Lives;
float X, Y, PrevX, PrevY, XSpeed, YSpeed;
long Time, FilledPixels = 0;
char GoOut, YouAreDead = 0, MustExit = 0;/* Boolean*/
char KeyRead, FillColor;
char c[40];
float OldTime;
//void interrupt ( * oldinterrupt)(...);


BITMAP *Ball;
BITMAP *EnemyBall;
//BITMAP *Under;
//BITMAP *UnderEnemy[10];

BITMAP *RealScreen;
//BITMAP *tempbuf;

FONT *font;

#include "ARNDWALL.C"

double sqr(double x)
{
  return (x*x);
}



End()
{
  char i;
  destroy_bitmap (Ball);
  destroy_bitmap (EnemyBall);
  remove_keyboard();
  remove_timer();
  allegro_exit();
  exit(0);
}






int NewFill (int XX, int YY)
{
  int FirstX, LastX;

  for (LastX = XX; GetRealPixel(LastX,YY) == 0; LastX++);
  LastX--;

  for (FirstX = XX; GetRealPixel(FirstX,YY) == 0; FirstX--);
  FirstX++;
  FilledPixels += LastX - FirstX;

  hline (screen, FirstX, YY, LastX, FillColour);
  hline (RealScreen, FirstX, YY, LastX, FillColour);

  XX = FirstX;
  while (XX <= LastX)
  {
    if (GetRealPixel(XX,YY + 1) == 0) XX = NewFill (XX, YY + 1); else XX++;
  }


  XX = FirstX;
  while (XX <= LastX)
  {
    if (GetRealPixel(XX,YY - 1) == 0) XX = NewFill (XX, YY - 1); else XX++;
  }
  return (LastX);
}






Inside_Outside(int X1, int Y1, int X2, int Y2, int *ReturnX1, int *ReturnY1, int *ReturnX2, int *ReturnY2)
{
  int X, Y, X0, Y0, PrevX, PrevY, NextX, NextY, i;
  char w;

  *ReturnX1 = X1;
  *ReturnY1 = Y1;
  *ReturnX2 = X2;
  *ReturnY2 = Y2;

 for (w = 0; w < BallNumber; w++)
 {
  Y = ball[w].Y;
//  putimage (ball[w].X - 4, ball[w].Y - 4, UnderEnemy[w], COPY_PUT);
  for (X = ball[w].X; (GetRealPixel (X, Y) == 0); X++)/* PutPixel (X, Y, 6)*/;
  X--;
  /*start walking along right wall*/
  PrevX = X;
  PrevY = Y + 1; /*as if from below*/
  X0 = X;
  Y0 = Y;
  do
  {
    GoAlongRightWall(X, Y, PrevX, PrevY, &NextX, &NextY, 0);

    PrevX = X;
    PrevY = Y;
    X = NextX;
    Y = NextY;

//    PutPixel (X, Y, 6); delay (2);

    if (X == X1 && Y == Y1) {*ReturnX1 = *ReturnY1 = 0;} /*false*/
    if (X == X2 && Y == Y2) {*ReturnX2 = *ReturnY2 = 0;} /*false*/

  } while (!( /*(X == X1 && Y == Y1) || (X == X2 && Y == Y2) ||*/ (X == X0 && Y == Y0)));

 } /*for w++*/
}




Initialize()
{
  char i;
//  cleardevice();
  clear(screen);
  clear(RealScreen);
  X = MAXX / 2;
  Y = 10;
  XSpeed = YSpeed = 0;

  rect (RealScreen, 0, 0, MAXX-1, MAXY-1, FillColour);
  for (I = 22; I <= MAXX - 22; I++)
  {
    PutRealPixel (I, sin(I/10.)*4.0 + 20, LineColour);
    PutRealPixel (I, MAXY - sin(I/10.)*4.0 - 20, LineColour);
  }
  for (I = 22; I <= MAXY - 22; I++)
  {
    PutRealPixel (sin(I/10.0)*4.0 + 20, I, LineColour);
    PutRealPixel (MAXX - sin(I/10.0)*4.0 - 20, I, LineColour);
  }
  NewFill (2, 2);
//  blit (screen, Under, 1, 1, 0, 0, BallSize + 2, BallSize + 2);
  FilledPixels = 0;

  textout (RealScreen, font, "Filled: ", 50, MAXY - 12, LineColour);
  blit (RealScreen, screen, 0, 0, 0, 0, MAXX, MAXY);
  for (i = 0; i < BallNumber; i++)
  {
    ball[i].X = Random(MAXX-200) + 100;
    ball[i].Y = Random(MAXY-200) + 100;
//    ball[i].X = i * 20 + 100;
//    ball[i].Y = i * 20 + 100;

    ball[i].XSpeed = Random(TotalSpeed*100.0) / 100.0;
//    ball[i].XSpeed = TotalSpeed/2;
    ball[i].YSpeed = sqrt(sqr(TotalSpeed) - sqr(ball[i].XSpeed));

//    ball[i].XSpeed *= ( (Random(1.9999999) + 1) * 2) - 3;
//    ball[i].YSpeed *= ( (Random(1.99999999) + 1) * 2) - 3;

//    blit (screen, UnderEnemy[i], ball[i].X - 4, ball[i].Y - 4, 0, 0, 8, 8);
    blit (EnemyBall, screen, 0, 0, ball[i].X - 4, ball[i].Y - 4, 10, 10);
  }
}





YouDie()
{
  int i, ii;

  PutUnderImage((PrevX - BallSize / 2) - 1, (PrevY - BallSize / 2) - 1, BallSize + 2, BallSize + 2);

  for (ii = 0; ii < 3; ii++)
  {
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 12); delay(5);
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 14); delay(10);
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 15); delay(20);
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 14); delay(10);
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 12); delay(5);
    for (i = 0; i < YourPixel; i++) PutPixel (YourLine[i].X, YourLine[i].Y, 0); delay (20);
  }

  for (i = 0; i < YourPixel; i++)
  {
    PutPixel (YourLine[i].X, YourLine[i].Y, 0);
    putpixel (RealScreen, YourLine[i].X, YourLine[i].Y, 0);
  }

  X = MAXX / 2;
  Y = 10;
  X = MAXX / 2;
  Y = 10;
  XSpeed = YSpeed = 0;
  sound (440); delay (200); nosound();

  Lives--;
  sprintf (c, "Lives: %u ", Lives);
  textout(screen, font, c, MAXX - 100, MAXY-10, LineColour);


}







DoTheFilling()
{
 char OK;
 int i, j, x1, x2, y1, y2;

//  2 good possibilities:
//  1)
     i=0;

     OK = 0;

     do
     {
       if ( (GetRealPixel(YourLine[i].X+1, YourLine[i].Y) == 0)
         && (GetRealPixel(YourLine[i].X-1, YourLine[i].Y) == 0) ) //vertical
           {
             x1 = YourLine[i].X + 1;
             x2 = YourLine[i].X - 1;
             y1 = YourLine[i].Y;
             y2 = YourLine[i].Y;
             OK = 1;
           }
       else
//   2)
         if ( GetRealPixel(YourLine[i].X, YourLine[i].Y+1) == 0
         && GetRealPixel(YourLine[i].X, YourLine[i].Y-1) == 0) //horizontal
           {

             x1 = YourLine[i].X;
             x2 = YourLine[i].X;
             y1 = YourLine[i].Y + 1;
             y2 = YourLine[i].Y - 1;
             OK = 1;
           }
      if (OK)
      {
        Inside_Outside(x1, y1, x2, y2, &x1, &y1, &x2, &y2);

	if (x1 != 0) {NewFill (x1, y1);}
        if (x2 != 0) {NewFill (x2, y2);}
        if (x1 == 0 && x2 == 0) break;
        OK = 0;
      }

      i++;
    } while (i < YourPixel - 1);


  for (i = 0; i < YourPixel; i++)
  {
    PutPixel (YourLine[i].X, YourLine[i].Y, LineColour);
    putpixel (RealScreen, YourLine[i].X, YourLine[i].Y, LineColour);
    FilledPixels++;
  }

//  blit (screen, Under, (X - BallSize / 2) - 1, (Y - BallSize / 2) - 1, 0, 0, BallSize + 2, BallSize + 2);

  i = FilledPixels*100/(MAXX-30)/(MAXY-30);

  sprintf (c, "%u ", i);
  rectfill (screen, 110, MAXY - 12, 150, MAXY, FillColour);
  textout (screen, font, c, 110, MAXY - 12, LineColour);
  textout (screen, font, "%", 130, MAXY - 12, LineColour);
  blit (screen, RealScreen, 110, MAXY - 12, 110, MAXY - 12, 40, 16);
}





MakeSound()
{
  int i;
  for (i = 200; i <= 1000; i++)
  {
    sound (i);
    delay(1);
  }

  nosound();

}




YouMovingStuff()
{
  if (key[77])
  {
    XSpeed++;                            /*  <---------   */
//    exit(36);
  }
  if (key[75]) XSpeed--;
  if (key[80]) YSpeed++;
  if (key[72]) YSpeed--;
/*   if XSpeed >== 0 then XSpeed = XSpeed - 0.4 else XSpeed = XSpeed + 0.4;
    if YSpeed >== 0 then YSpeed = YSpeed - 0.4 else YSpeed = YSpeed + 0.4; */
  PrevX = X;
  PrevY = Y;
  if (XSpeed >= 0)
    XSpeed = XSpeed - sqr(XSpeed / Friction) - ExtraFr; else
      XSpeed = XSpeed + sqr(XSpeed / Friction) + ExtraFr;
  if (YSpeed >= 0)
    YSpeed = YSpeed - sqr(YSpeed / Friction) - ExtraFr; else
      YSpeed = YSpeed + sqr(YSpeed / Friction) + ExtraFr;
  if ((X > 2 + BallSize / 2) && (X < MAXX - BallSize / 2))
    X += XSpeed * Acceleration;
    else
    {
      XSpeed = XSpeed * -1 * Bounce;
      X += XSpeed/fabs(XSpeed);
      X += XSpeed * Acceleration;
    }
  if ((Y > 2 + BallSize / 2) && (Y < MAXY - BallSize / 2))
    Y += YSpeed * Acceleration;
    else
    {
      YSpeed = YSpeed * -1 * Bounce;
      Y += YSpeed / fabs(YSpeed);
      Y += YSpeed * Acceleration;
    }
}







EnemyMovingStuff()
{
  int LeftX, RightX, LeftY, RightY, x, y, prevX, prevY, NextX, NextY;
  char i, w, ww;
  float Gradient, Angle, XSpeed, XSpeed0, YSpeed0, X0, Y0;
  struct {float X, Y;} Prev[10];

  if (key[1]) End();

  for (w = 0; w < BallNumber; w++)
  {
    Prev[w].X = ball[w].X;
    Prev[w].Y = ball[w].Y;
//    if (equals( GetPixel(ball[w].X, ball[w].Y), FillColour, LineColour))
//      {textout (screen, font, "GAGAGAG, INSIDE!!!!sdsdasdasda>><><", 20, 20, LineColour); getch();}

    ball[w].X += ball[w].XSpeed;
    ball[w].Y += ball[w].YSpeed;

    /*check for bouncing*/
    for (ww = w + 1; ww < BallNumber; ww++)
      if (abs(ball[w].X - ball[ww].X) < 10 && abs(ball[w].Y - ball[ww].Y) < 10)
      {
         ball[w].X -= ball[w].XSpeed;
         ball[w].Y -= ball[w].YSpeed;
         ball[ww].X -= ball[ww].XSpeed;
         ball[ww].Y -= ball[ww].YSpeed;
         XSpeed0 = ball[w].XSpeed;
         YSpeed0 = ball[w].YSpeed;
         ball[w].XSpeed = ball[ww].XSpeed;
         ball[w].YSpeed = ball[ww].YSpeed;
         ball[ww].XSpeed = XSpeed0;
         ball[ww].YSpeed = YSpeed0;
       }
   /*end check of bouncing*/

  /*if it has actually moved 1 pixel*/
    if ( ((int)Prev[w].X) != ((int)ball[w].X) || ((int)Prev[w].Y) != ((int)ball[w].Y))
    {
      XSpeed0 = ball[w].XSpeed;
      YSpeed0 = ball[w].YSpeed;
      X0 = ball[w].X;
      Y0 = ball[w].Y;

#ifdef normal
      PutUnderImage(Prev[w].X - 4, Prev[w].Y - 4, 8, 8);
#endif

      if (GetRealPixel (ball[w].X, ball[w].Y) == 4) {YouAreDead = 1;}

      if (equals( GetRealPixel(ball[w].X, ball[w].Y), FillColour, LineColour))
      {
        ball[w].X = Prev[w].X;
        ball[w].Y = Prev[w].Y;

        LeftX = x = (ball[w].X);
        LeftY = y = (ball[w].Y);
        /*right wall*/
        if (equals(GetRealPixel (x, y - 1) /*above*/, FillColour, LineColour)) {prevX = x + 1; prevY = y;}
        else
        if (equals(GetRealPixel (x, y + 1) /*below*/, FillColour, LineColour)) {prevX = x - 1; prevY = y;}
        else
        if (equals(GetRealPixel (x + 1, y) /*right*/, FillColour, LineColour)) {prevX = x; prevY = y + 1;}
        else
        if (equals(GetRealPixel (x - 1, y) /*left*/, FillColour, LineColour)) {prevX = x; prevY = y - 1;}
        else
        if (equals(GetRealPixel (x - 1, y - 1) /*above-left*/, FillColour, LineColour)) {prevX = x; prevY = y - 1;}
        else
        if (equals(GetRealPixel (x - 1, y + 1) /*below-left*/, FillColour, LineColour)) {prevX = x - 1; prevY = y;}
        else
        if (equals(GetRealPixel (x + 1, y - 1) /*above-right*/, FillColour, LineColour)) {prevX = x + 1; prevY = y;}
        else
        if (equals(GetRealPixel (x + 1, y + 1) /*below-right*/, FillColour, LineColour)) {prevX = x; prevY = y + 1;}
        else
        {textout(screen, font, " STRANGE!  GAGAGAGAG", 20, 20, LineColour); getch(); exit(1);}
        for (i = 0; i <= 3; i++)
        {
          if (prevX == x && prevY == y) {textout(screen, font, "Silly and GAGAG", 20, 20, LineColour); getch(); exit(1);}
          GoAlongRightWall(x, y, prevX, prevY, &NextX, &NextY, 0);
          prevX = x;
          prevY = y;
          x = NextX;
          y = NextY;
        }; /*for*/
        RightX = x;
        RightY = y;
     /*left wall*/
        x = LeftX;
        y = LeftY;
        if (equals(GetRealPixel (x, y - 1) /*above*/, FillColour, LineColour)) {prevX = x - 1; prevY = y;}
        else
        if (equals(GetRealPixel (x, y + 1) /*below*/, FillColour, LineColour)) {prevX = x + 1; prevY = y;}
        else
        if (equals(GetRealPixel (x + 1, y) /*right*/, FillColour, LineColour)) {prevX = x; prevY = y - 1;}
        else
        if (equals(GetRealPixel (x - 1, y) /*left*/, FillColour, LineColour)) {prevX = x; prevY = y + 1;}
        else
        if (equals(GetRealPixel (x - 1, y - 1) /*above-left*/, FillColour, LineColour)) {prevX = x - 1; prevY = y;}
        else
        if (equals(GetRealPixel (x - 1, y + 1) /*below-left*/, FillColour, LineColour)) {prevX = x; prevY = y + 1;}
        else
        if (equals(GetRealPixel (x + 1, y - 1) /*above-right*/, FillColour, LineColour)) {prevX = x; prevY = y - 1;}
        else
        if (equals(GetRealPixel (x + 1, y + 1) /*below-right*/, FillColour, LineColour)) {prevX = x + 1; prevY = y;}
        else
          {textout(screen, font, " STRANGE!  GAGAGAGAG", 20, 20, LineColour); getch(); exit(1);}
        for (i = 0; i <= 3; i++)
        {
          if (prevX == x && prevY == y) {textout(screen, font, "Silly and GAGAG", 20, 20, LineColour); getch(); exit(1);}
          GoAlongLeftWall(x, y, prevX, prevY, &NextX, &NextY, 0);
          prevX = x;
          prevY = y;
          x = NextX;
          y = NextY;
        }; /*for*/
        LeftX = x;
        LeftY = y;
        Gradient = (RightY - LeftY)/(RightX - LeftX + 0.0001);
        Angle = atan(Gradient);

        XSpeed = ball[w].XSpeed;
        ball[w].XSpeed = cos(2*Angle)*ball[w].XSpeed + sin(2*Angle)*ball[w].YSpeed;
        ball[w].YSpeed = sin(2*Angle)*XSpeed - cos(2*Angle)*ball[w].YSpeed;

/*    ball[w].XSpeed += Random (100) / 100;
     ball[w].YSpeed += Random (100) / 100;*/

        sound (200); delay(1); nosound();

        if (GetRealPixel(ball[w].X + ball[w].XSpeed, ball[w].Y + ball[w].YSpeed) != 0)
        {
          while (GetRealPixel(ball[w].X + ball[w].XSpeed, ball[w].Y + ball[w].YSpeed) != 0 && sqr(ball[w].X-X0)+sqr(ball[w].Y-Y0) < 36)
          {

            ball[w].X += ball[w].XSpeed; // fabs(ball[w].XSpeed);
            ball[w].Y += ball[w].YSpeed; // fabs(ball[w].YSpeed);

          }/*while*/
          if (sqr(ball[w].X-X0)+sqr(ball[w].Y-Y0) > 25)
          {
            ball[w].X = X0;
            ball[w].Y = Y0;
            ball[w].XSpeed = XSpeed0;
            ball[w].YSpeed = YSpeed0;

            while (GetRealPixel(ball[w].X + ball[w].XSpeed, ball[w].Y + ball[w].YSpeed) != 0 && (sqr(ball[w].X-X0)+sqr(ball[w].Y-Y0) < 100))
            {
              ball[w].X += ball[w].XSpeed; // fabs(ball[w].XSpeed);
              ball[w].Y += ball[w].YSpeed; // fabs(ball[w].YSpeed);
            }
            if (sqr(ball[w].X-X0)+sqr(ball[w].Y-Y0) > 89)
  	    {
              textout (screen, font, "Also too far, GAGAGAGGA", MAXX - 200, MAXY-10, 1);
              ball[w].X = X0;
              ball[w].Y = Y0;
    //          ball[w].XSpeed = -XSpeed0+Random(100)/10000.0-100/20000.0;
    //          ball[w].YSpeed = sqrt(fabs(sqr(TotalSpeed) - sqr(ball[w].XSpeed))) * -(YSpeed0/fabs(YSpeed0));
              Gradient = -1/Gradient;
              ball[w].XSpeed = sqrt( sqr(TotalSpeed) / (1 + sqr(Gradient) ) );
              ball[w].YSpeed = ball[w].XSpeed * Gradient;
              if ( equals(GetRealPixel(ball[w].X + ball[w].XSpeed * 5, ball[w].Y + ball[w].YSpeed * 5), FillColour, LineColour) )
              {
                ball[w].XSpeed = -sqrt( sqr(TotalSpeed) / (1 + sqr(Gradient) ) );
                ball[w].YSpeed = ball[w].XSpeed * Gradient;
              }

            }
          }/*if*/
          ball[w].X += ball[w].XSpeed; // fabs(ball[w].XSpeed);
          ball[w].Y += ball[w].YSpeed; // fabs(ball[w].YSpeed);
        }/*if*/
      }/*BIG IF (must bounce...) */

      draw_sprite (screen, EnemyBall, ball[w].X - 4, ball[w].Y - 4);

    } /*if ssss*/
  } /*for w++ */
}

			/*--------------------*/
                        /*--------------------*/
			/*--------------------*/
                        /*--------------------*/
			/*--------------------*/
                        /*--------------------*/



PlayLevel()
{
  char i;
  do
  {
    do                    /*inside your territory*/
    {
      for (I = 1; I <= Wait1; I++) 1;
//      rest(1);
//      if (kbhit()) getch();

      if (key[1]) End();
      EnemyMovingStuff();
      YouMovingStuff();
      if ((int)PrevX != (int)X || (int)PrevY != (int)Y)
      {
        PutUnderImage((PrevX - BallSize / 2) - 1, (PrevY - BallSize / 2) - 1, BallSize + 2, BallSize + 2);

        if ((GetRealPixel(X, Y) != FillColour) && (GetRealPixel(X, Y) != LineColour))
        {
	  GoOut = 1;
          PutPixel (X, Y, 4); putpixel (RealScreen, X, Y, 4);
        }
	else
          draw_sprite (screen, Ball, ((int)(X - BallSize / 2) - 1), ((int)(Y - BallSize / 2) - 1));

      } /*if*/
    } while (!GoOut);
    GoOut = 0;

    YourLine[0].X = X;
    YourLine[0].Y = Y;
    YourPixel = 1;

    do                            /*outside your territory*/
    {
      for (I = 1; I <= Wait2; I++) 1;
//      rest(1);
//      if (kbhit()) getch();

      if (key[1]) End();
      EnemyMovingStuff();
      YouMovingStuff();
      if (YouAreDead)
      {
        YouDie();
        GoOut = 1;
        break;
      }
      if ((int)PrevX != (int)X || (int)PrevY != (int)Y)
      {
        PutUnderImage((PrevX - BallSize / 2) - 1, (PrevY - BallSize / 2) - 1, BallSize + 2, BallSize + 2);

        if ((GetRealPixel(X, Y) == FillColour) || (GetRealPixel(X, Y) == LineColour))
	{
	  GoOut = 1;
          PutPixel (X, Y, LineColour); putpixel (RealScreen, X, Y, LineColour);
        }
        else
        {
          PutPixel (X, Y, 4);  putpixel (RealScreen, X, Y, 4);
          draw_sprite (screen, Ball, (X - BallSize / 2) - 1, (Y - BallSize / 2) - 1);
          YourLine[YourPixel].X = X;
          YourLine[YourPixel].Y = Y;
          YourPixel++;
        }

      } /*if*/
    } while (!GoOut);
    GoOut = 0;

    if (!YouAreDead) DoTheFilling();
    YouAreDead = 0;

   } while (FilledPixels < (0.75 * (MAXX - 30) * (MAXY - 30)) && Lives != 0) ;
}






/*------------------------------------------------------------------------*/
/*------------------------------------------------------------------------*/
/*------------------------------------------------------------------------*/
/*------------------------------------------------------------------------*/
/*------------------------------------------------------------------------*/









main()
{
  char i;

  allegro_init();
  set_gfx_mode (GFX_VESA1, MAXX, MAXY, 0, 0);
  clear(screen);
  install_keyboard();
  install_timer();
  text_mode(-1);

  srandom(time(0));

  Ball = create_bitmap(BallSize + 2, BallSize + 2);/**/
  EnemyBall = create_bitmap(10, 10);/**/
  RealScreen = create_bitmap(MAXX, MAXY);

  clear(EnemyBall);
#ifdef normal
  ellipsefill (EnemyBall, 4, 4, 3, 3, 12);
  ellipse (EnemyBall, 4, 4, 3, 3, 15);
#else
  putpixel (EnemyBall, 4, 4, 5);
#endif
  clear (Ball);
  circle (Ball, BallSize / 2 + 1, BallSize / 2 + 1, BallSize / 2, 15);
  line (Ball, 1, BallSize / 2+1, BallSize, BallSize / 2+1, 15);
  line (Ball, BallSize / 2+1, 1, BallSize / 2+1, BallSize, 15);
  circlefill (Ball, BallSize / 2 + 1, BallSize / 2 + 1, BallSize / 2-3, 0);
  circle (Ball, BallSize / 2 + 1, BallSize / 2 + 1, BallSize / 2-3, 15);

  Level = 1;
  Lives = LivesNum;

  while (Lives != 0)
  {
    BallNumber = 2 + Level;
    Initialize();
    sprintf (c, "Lives: %u ", Lives);
    textout(screen, font, c, MAXX - 100, MAXY-12, LineColour);
    readkey();
    PlayLevel();
    MakeSound();
    Level++;
  }
  End();
}








