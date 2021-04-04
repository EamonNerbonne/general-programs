#define GGGetpixel(x,y) GetRealPixel(x,y)

GoAlongRightWall(int X, int Y, int PrevX, int PrevY, int *NextX, int *NextY, char Colour)
{
//  blit(screen, tempbuf, 0, 0, 0, 0, MAXX, MAXY);

  if (PrevX < X)  /*from left ---> */
    {
      if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
        else
        if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
          else
	  if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
	    else
	    if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
    else
    if (PrevX > X)  /*from right <--- */
    {
      if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
        else
        if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
          else
	  if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
            else
            if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
              else
	      {textout(screen, font, "GAGAGGAGAGA from right --- ?", 100, 100, LineColour);
//               putpixel (X, Y, 12);
	       getch();
	       /*exit(1);*/}
//      printf(" ---out of all ifs--- ");
    }
    else
    if (PrevY < Y)  /*from above  V */
    {
      if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
        else
        if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
          else
	  if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
            else
	    if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
    else
    if (PrevY > Y)  /*from below  ^ */
    {
      if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
        else
        if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
          else
	  if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
	    else
	    if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
}



GoAlongLeftWall(int X, int Y, int PrevX, int PrevY, int *NextX, int *NextY, char Colour)
{
//  blit(screen, tempbuf, 0, 0, 0, 0, MAXX, MAXY);
    if (PrevX < X)  /*from left ---> */
    {
      if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
        else
        if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
          else
	  if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
	    else
	    if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
    else
    if (PrevX > X)  /*from right <--- */
    {
      if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
        else
        if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
          else
	  if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
            else
            if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
    else
    if (PrevY < Y)  /*from above  V */
    {
      if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
        else
        if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
          else
	  if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
            else
	    if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
    else
    if (PrevY > Y)  /*from below  ^ */
    {
      if (GGGetpixel(X - 1, Y) == Colour) {*NextX = X - 1; *NextY = Y;}
        else
        if (GGGetpixel(X, Y - 1) == Colour) {*NextX = X; *NextY = Y - 1;}
          else
	  if (GGGetpixel(X + 1, Y) == Colour) {*NextX = X + 1; *NextY = Y;}
	    else
	    if (GGGetpixel(X, Y + 1) == Colour) {*NextX = X; *NextY = Y + 1;}
        else {printf("GAGAGGAGAGA"); getch(); exit(1);}
    }
}
