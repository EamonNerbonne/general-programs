#include <stdio.h>
#include <stdlib.h>
unsigned mycrc(FILE*stream, unsigned poly) {
  unsigned reg=0,c;
  for (c=fgetc(stream); c!=EOF; c=fgetc(stream) ) {
     int   i;
     reg ^= (c << (32-8));
     for (i=0; i<8; i++)  reg = reg & (1<<31) ? (reg << 1) ^ poly : (reg << 1);
  }
  return reg;
}

int main(int argc, char** argv) {
  if (argc!=2 && argc !=3) {
    printf("Usage: calccrc <poly> [<filename>]\n"
	   "calccrc calculates the crc of standard in, and writes that to standard output.");
     return 1;
  }
  printf("0x%x\n",mycrc(argc==2?stdin:fopen(argv[2],"r"),strtoul(argv[1],0,0)));
  return 0;
}
