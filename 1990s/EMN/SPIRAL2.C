#include <conio.h>
#include <math.h>
#include <pc.h>
#include <allegro.h>

#define MAXX 800
#define MAXY 600

double X, Y, Angle, Radius, c;
//char modthing = 0;
BITMAP *buffer;
//MENU *menu;


main()
{
  int type;
  clrscr();
  printf ("\n Choose Spiral (1-5) \n");
  printf ("\n 1: Many spirals");
  printf ("\n 2: Strange not spiral");
  printf ("\n 3: Flowers (2 spirals)");
  printf ("\n 4: Flowers 2");
  printf ("\n 5: Chaos spiral");
  printf ("\n 6: Strange maybe spirals");
  printf ("\n 7: Waves");
  printf ("\n");

  type = getch() - 48;

  allegro_init();
  install_keyboard();
  set_gfx_mode (GFX_VESA1, MAXX, MAXY, 0, 0);
  clear(screen);
  buffer = create_bitmap(MAXX, MAXY);
  clear(buffer);

  //  menu.proc = "gagb";
 // do_menu(menu, 100, 100);

  switch (type)
  {
    case (1): ManySpirals(); break;
    case (2): StrangeNotSpiral(); break;
    case (3): FlowerSpirals(); break;
    case (4): FlowerSpirals2(); break;
    case (5): ChaosSpiral(); break;
    case (6): TurningSpiral(); break;
    case (7): TurningChaosSpiral(); break;
    default: exit(36);
  }
}


/*------------------------------------------------------------------------*/



/* 1: */
ManySpirals()
{
  c = 0.005;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 2 == 1)
        Angle = Angle + c;/**/
        else Angle = Angle -c*c;
      Radius += 0.07;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
    {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.0002;
  } /*for;;*/
}









/* 2: */
StrangeNotSpiral()
{
  c = 0.005;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 2 == 1)
        Angle = Angle + 0.01;/**/
        else Angle = -Angle;

        Radius += c;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
    {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.00002;
  } /*for;;*/
}





/* 3: */
FlowerSpirals()
{
  c = 0.005;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 2 == 0)
        Angle = -Radius*(1+c);/**/
        else Angle = Radius*(1+c);
//        else Angle = Radius - 0.1*c;
      Radius = Radius+0.5;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
    {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.0002;
  } /*for;;*/
}



/* 4: */
FlowerSpirals2()
{
  c = 0.01;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 2 == 0)
        Angle = -Radius*(1+c);/**/
        else Angle = Radius*(1+c);
//        else Angle = Radius - 0.1*c;
      Radius = Radius+0.1;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
    {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.0002;
  } /*for;;*/
}





/* 5: */
ChaosSpiral()
{
  c = 0.005;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius+Angle) % 2 == 0)
        Angle += Radius*c*1.01 + 0.1;/**/
//        else Angle = Radius * Angle -c*c;
        else Angle -= Radius*c - 0.1;
      Radius = Radius + 14 + Angle*0.1;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
    {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.0002;
  } /*for;;*/
}





/* 6: */
TurningSpiral()
{
  c = 5;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 3 == 1)
        Angle = Radius*c*0.002;/**/
        else Angle -= Radius/Angle-10*c;
      Radius *= 1.0008;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
     {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.002;
  } /*for;;*/
}

/* 7: */
TurningChaosSpiral()
{
  c = 5;
  for(;;)
  {
    clear(buffer);
    X = 0.1;
    Y = 0.1;
    Radius = 0.001;
    Angle = 0.1;
    do
    {
      X = Radius * cos(Angle);
      Y = Radius * sin(Angle);
      putpixel (buffer, MAXX/2 + X, MAXY/2 + Y, 15);

      if ((int)(Radius) % 3 == 1)
        Angle += Angle/Radius*0.0010;/**/
        else Angle -= Radius/Angle+c;
      Radius *= 1.0008;
    }
    while (Radius < 500);

    blit (buffer, screen, 0, 0, 0, 0, MAXX, MAXY);
    if (keypressed())
     {
      if ((readkey() & 0xff) == 'p') readkey();
      else exit(0);
    }
    c += 0.002;
  } /*for;;*/
}

