/*
This is a program to sort (large) arrays.  It is an unhealthy mix of c and c++.
It is ansi c completely, excluding getch, more on that in the #include section.
So if you want to recompile this under UNIX, go to the very beginning of the
program and uncomment the "#define NOCONIO" line.


PSEUDOCODE
really pseudo-idea, but heck.


SelectionSort Pseudo code.
WARNING - OFFENSIVELY INEFFICIENT ALGORITHM!
Find the smallest value in the whole array.
Swap with the first position.
Find the smallest value in the array excluding the first element.
Swap with the second positon.
Repeat as often as necessary.


BubbleSort Pseudo code.
WARNING - VERY OFFENSIVELY INEFFICIENT ALGORITHM!
for <entire array>
  for <entire array>
    swap that element with the next if the next one's smaller.
In other words, BRUTE FORCE.
It just swaps consecutive elements if necessary, very often.


TwitchSort Pseudo code.
I love that name.  >TWITCH<
It is comparable to the "two finger's and ruler" sort of idea:
You put both your fingers under the far ends of a ruler, holding it like that.
You now simply push towards the middle, and occasionally the one or the other
finger will move, but they always, irrelevant of frictional coefficients,
arrive together in the middle of the ruler.
Anyway:

Choose arbitrary midvalue for your array.
Go along the array from the beginning using a "pointer", and stop whenever you
hit a value that is larger than the midpoint.
Go along the array from the _end_ using a "pointer", and stop whenever you hit
a value that is _smaller_ than the midpoint.
>TWITCH<  -  Swap those two values.
Continue doing so, alternating between pointers, until they meet in the middle.
Now you split the array in half, one side bigger than x, another smaller.  All
you need to do is repeat the process for the two halves, and the halves of the
halves, and so on, recursively, until you reach chunks of size one.  Then
you're done!  Ahhhh - >TWITCH<


MergeSort Pseudo code.
split array in half, right down the middle.  Continue splitting so splitted
splats, until the splats are one in size.  Then zip up the zipper again:
choose the smaller of the one sized chunks and turn it into the first value of
a two sized chunk.   Take the larger one and make it the second element of a
two-sized chunk.  Two sized chunks can be joined similarly: take the smaller
of the first elements and make it the first element of a four sized one.  Then
compare the second element of the used chunk and the first of the unused array
and so on.

Like this:
we have

1 3 7 16 17 and 2 2 4 9 10 19 21 22

1            <  2                   => 1
  3          >  2                   => 2
  3          >    2                 => 2
  3          <      4               => 3
    7        >      4               => 4
    7        <        9             => 7
      16     >        9             => 9
      16     >          10          => 10
      16     <             19       => 16
          17 <             19       => 17
                           19       => 19
                              21    => 21
                                 22 => 22
you get it?


MemorySort Pseudo code.
It is really simple and FAST, but'll only work for discreet types.  Like int's.
You simply count how many 0's, how many 1's, how many 2's ... how many 32767's
and then make an array that starts with n 0's, n 1's, n 3's etc.
Really easy.  However, it is linear in nature, so exceptionally well suited for
huge arrays of integers.





Version History

1.0b-1.99 A basic sorting program, really pretty simple.
2.0b      New design of sorting algorithm, intended to make it simple to
          implement new algorithms.
2.1b      split into more functions to gain an overview in main.
2.11      Selection Sort implemented.  Only three statements long!!!
2.20      Attempt to further restructure the program, Twitch Sort attempted and
          failed.
2.21      Twitch sort now basically works, but occasionally will not sort
          correctly and causes stack overflows since it's recursive.
2.22      Twitch algorithm corrected.  The midpoint search was incorrect.  The
          algorithm gave wrong results when the midpoint was in a BIG gap on
          the number spread, which is esp. likely to occur with small arrays.
2.22      Twitch now has it's own stack, and is really no longer technically
          recursive. It's also illegible because of the manual stack calls.
          Horrible.  Also, Twitch again causes false sorting and it freezes on
          big arrays, probably since the stack isn't dynamically enlarged.
2.23      Stack can now be dynamically enlarged, but that didn't do any good,
          it still screws up!  Also figured out why, even: the stack is usually
          no more than log(2,n) large where n is the array size, though it can
          be worse.
2.24      Twitch now no longer messes up.  The messing up was caused by
          duplicate numbers.  The algorithm didn't split those right at all.
2.24      The midpoint must be rounded up, not down, to force correct sorting,
          otherwise duplicate numbers cause the program to freeze, since it is
          unable to split two consecutive numbers correctly.  Twitch works!
2.31      Implementing merge sort.
2.32      Merge sort implemented.  Crashes at n=2, stack overflow at n=3.
          Great.
2.33      Corrected stack overflow by custom stack, now he just crashes.
2.34      There was an error in the Merge algorithm, really simple, he now
          no longer crashes but only goes into an infinite loop. at any n>5,
          Sucks.
2.35      Infinite loop problem corrected.
2.36      Several optimizations to improve the speed, and also a DEBUG
          definition to allow simple debugging of the programs actions.  Also,
          There now is a check to see if the array really was sucessfully
          sorted.  Merge doesn't quite work, apparently.
2.37      Merge fixed.  Since it requires copying large amounts of data, it is
          slower that Twitch sort at arrays of above about 200 000 elements in
          size.  Beneath that, it is quite substantially faster, like a factor
          of 5 or more the smaller it gets.  Twitch never is much faster, maybe
          20 percent with huge (over 2 000 000 elements) arrays.
2.38      Now has preprocessor settings for those of you using unix... No
          getch()'s, that is.
*/


#define VERSION "2.38"
//#define DEBUG
//#define DEBUG_PLUS
//#define NOCONIO
//#define WAITONEXIT

// If you define a DEBUG, certain debugging info will be printed to cout.
// DEBUG_PLUS is a bit more rigourous and will also and some getch()'s as
// necessary.  Both DEBUG's will only affect MergeSort, since I feel that way.
// NOCONIO should be uncommented for those running unix, as we must then
// make due without the direct console access, conio.h
// WAITONEXIT will cause the program to wait on exit.  This makes sure you can
// read what the program is saying if you are running under windows or X and
// the windows would otherwise close immediately.


#include <iostream.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#ifdef NOCONIO
int __WHATEVER_;
#define getch() scanf("%d",&__WHATEVER_)
#else
#include <conio.h>
#endif


#define MAX_VAL 32768
#define MIN_VAL -1
#define ErrorExit(x,y) ErrorTerminate(x,y,__LINE__)
#ifdef  DEBUG_PLUS
#define DEBUG
#endif

typedef struct
{
    int _begin,_end,_min,_max;
}  TwitchT;

typedef struct
{
    int _begin,_end;
}  MergeT;


void DisplayInfo(void);
int  ChooseArray(void);
int* GenerateArray(int number);
int  ChooseAlgorithm(void);
void ErrorTerminate(char*str,int err,int line);
int  Sort(int*data,int elements,int algorithm);
void Analyze(int elapsed);
int  Sorted(int*data,int elements);
void Swap(int*data,int elementA, int elementB);
int  FindSmallest(int*data,int begin,int end);
int  FindSmallestValue(int*data,int begin,int end);
int  FindLargestValue(int*data,int begin,int end);
void SelectionSort(int*array,int elements);
void BubbleSort(int*array,int elements);
void TwitchSort(int*data,int elements);
void TwitchSortREC(int*data,int begin,int end,int min,int max);
void MergeSort(int*data,int elements);
void MergeSortREC(int*data,int*databk,int begin,int end);
void MemorySort(int*data,int numelements);

void main(void)
{
    int numelements,algorithm;
    int *data;
    int elapsed;
#ifdef DEBUG    
    int I;
#endif

                   srand(time(0));//initialize random number generator
                   DisplayInfo();//Show Bullshit about program
    numelements=   ChooseArray();//Choose array size
    data=          GenerateArray(numelements);//generate array of requested size
#ifdef DEBUG
    cout <<"The array to be sorted: ";    
    for(I=0;I<numelements;I++)
        cout<<data[I]<<"  ";
    cout<<endl;
#endif
    algorithm=     ChooseAlgorithm();//choose sorting algorithm
    elapsed=       Sort(data,numelements,algorithm);//sort with chosen algorithm
                   Sorted(data,numelements);//check if the array was really successfully sorted or not.
                   Analyze(elapsed);//print out elapsed time

#ifdef DEBUG_PLUS
    getch();
    cout<<"The sorted array: "
    for(I=0;I<numelements;I++)
        cout<<data[I]<<"  ";
    cout<<endl;
#endif
#ifdef WAITONEXIT        
    getch();
#endif
    delete[]data;
}


void ErrorTerminate(char*str,int err,int line)
{
    cerr<<"\nError encountered on code line "<<line<<".\n"
          <<str<<"\nExiting with error "<<err<<".\n";
    if(!err) cerr<<"Internal error handling error: error case zero undefined\n";
#ifdef WAITONEXIT
    getch();
#endif
    exit(err);
}


void DisplayInfo(void)
{
    cout<< "This is a sorting program capable of sorting complex arrays of doubles with\n"
           "varying methods, timing each and returning the success of the different\n"
           "algorithms.\n"<<
           "Version "<<VERSION<< "\n\n";
}

int ChooseArray(void)
{
    char*inmessage;
    int innumber,errorstate;
    cout<< "Please enter the number of elements that you wish to sort: ";
    inmessage=new char[512];
    errorstate=scanf("%s",inmessage);
    if(errorstate!=1)ErrorExit("There has been an error reading the user input string.\nIMPORTANT, PLEASE NOTA BENE!.",1);
    errorstate=sscanf(inmessage," %d ",&innumber);
    if(errorstate!=1)ErrorExit("There has been an error parsing the user input string.\nIllegal data entry.",2);
    cout<<"\n";
    delete[]inmessage;
    return innumber;
}

int* GenerateArray(int number)
{
    int* data;
    int I,begin,end;
    cout<< "Generating array of size n="<<number<<", please wait, this may take a while...";
    begin=clock();
    data=new int[number];
    for (I=0;I<number;I++) data[I]=rand();
    end=clock();
    cout<<"done.\n";
    Analyze(end-begin);
    cout<<"\n";
    return data;
}

int ChooseAlgorithm(void)
{
    char*inmessage;
    int innumber,errorstate;
    cout<< "Which algorithm would you like to use?\n"
           "  1. Selection sort\n"
           "  2. Bubble sort\n"
           "  3. Twitch sort\n"
           "  4. Merge sort\n"
           "  5. Memory sort (small integers only)\n"
           "\n"
           "Anything else crashes back to OS.\n";
    inmessage=new char[512];
    errorstate=scanf("%s",inmessage);
    if(errorstate!=1)ErrorExit("There has been an error reading the user input string.\nIMPORTANT, PLEASE NOTA BENE!.",3);
    errorstate=sscanf(inmessage," %d ",&innumber);
    if(errorstate!=1)ErrorExit("There has been an error parsing the user input string.\nIllegal data entry.",4);
    cout<<"\n";
    delete[]inmessage;
    return innumber;
}

int  Sort(int*data,int numelements,int algorithm)
{
    int begin,end;
    cout<<"Sorting...";
    begin=clock();
    switch (algorithm)
    {
        case 1:SelectionSort(data,numelements);break;
        case 2:BubbleSort(data,numelements);break;
        case 3:TwitchSort(data,numelements);break;
        case 4:MergeSort(data,numelements);break;
        case 5:MemorySort(data,numelements);break;
        default:ErrorExit("Algorithm not implemented.",5);break;
    }
    end=clock();
    cout<<"done.\n";
    return end-begin;
}

void Analyze(int elapsed)
{
    cout<<"Elapsed time: "<< elapsed << " clock tics, or " << elapsed/double(CLOCKS_PER_SEC)<<" seconds.\n";
}

int  Sorted(int*data,int elements)
{
    int I;
    for(I=1;I<elements;I++) if(data[I]<data[I-1]) cout<<"Sorting mistake found at I="<<I<<endl
                                                      <<"data[I-1]="<<data[I-1]<<endl
                                                      <<"data[I]="<<data[I]<<endl;
    return 1;
}

void Swap(int*data,int elementA, int elementB)
{
    int tmp;
    tmp=data[elementA];
    data[elementA]=data[elementB];
    data[elementB]=tmp;
}

int  FindSmallest(int*data,int begin,int end)
{
    int val,num;
    val=MAX_VAL;
    do
    {
        if (data[begin]<val)
        {
            val=data[begin];
            num=begin;
        }
        begin++;
    }
    while(begin<end);
    return num;
}

void SelectionSort(int*data,int elements)
{
    int I,maxindex;
    maxindex=elements-1;
    for(I=0;I<maxindex;I++)
        Swap(data,FindSmallest(data,I,elements),I);
}

void BubbleSort (int *array,int length)
{
    int i, j;
    for (j = 0; j < length; j++)
      for (i = 0; i < length - 1; i++)
        if (array[i + 1] < array[i]) Swap (array,i, i + 1);
}


void TwitchSort(int*data,int elements)
{
    int min,max,I;
    min=MAX_VAL;
    max=MIN_VAL;
    I=0;
    do
    {
        if (data[I]<min) min=data[I];
        if (data[I]>max) max=data[I];
        I++;
    }
    while(I<elements);
    if(elements-0>1)TwitchSortREC(data,0,elements,min,max);
}

void TwitchSortREC(int*data,int begin,int end,int min,int max)
{
    int midway,markA,markB,up,down,I,count=0;
    TwitchT *stack,*tmp;
    int SSize,pointer;
    SSize=100;
    stack=new TwitchT[SSize];

    pointer=0;
    stack[pointer]._begin=begin;
    stack[pointer]._end=end;
    stack[pointer]._min=min;
    stack[pointer]._max=max;

    do
    {
        begin=stack[pointer]._begin;
        end=stack[pointer]._end;
        min=stack[pointer]._min;
        max=stack[pointer]._max;
        pointer--;
        
        midway= (min+max+1)>>1;
        markA=begin;
        markB=end-1;
        do
        {
            for(;(data[markA]<midway)&&(markA!=markB);markA++);
            for(;(data[markB]>=midway)&&(markA!=markB);markB--);
            Swap(data,markA,markB);
        }
        while(markA!=markB);
        if (markA-begin>1)
        {
            pointer++;
            stack[pointer]._begin=begin;
            stack[pointer]._end=markA;
            down=MAX_VAL;
            up=MIN_VAL;
            I=begin;
            do
            {
                if (data[I]<down) down=data[I];
                if (data[I]>up) up=data[I];
                I++;
            }
            while(I<markA);
            stack[pointer]._min=down;
            stack[pointer]._max=up;
        }
        if (end-markA>1)
        {
            pointer++;
            stack[pointer]._begin=markA;
            stack[pointer]._end=end;
            down=MAX_VAL;
            up=MIN_VAL;
            I=markA;
            do
            {
                if (data[I]<down) down=data[I];
                if (data[I]>up) up=data[I];
                I++;
            }
            while(I<end);
            stack[pointer]._min=down;
            stack[pointer]._max=up;
            if(down==up)pointer--;
        }
        if((SSize-pointer)<2)
        {
            tmp=stack;
            SSize+=100;
            stack=new TwitchT[SSize];
            memcpy(stack,tmp,(pointer+1)*sizeof(TwitchT));
            delete[]tmp;
        }
    }
    while (pointer>=0);
}

void MergeSort(int*data,int elements)
{
    int *databk;
    databk=new int[elements];
    MergeSortREC(data,databk,0,elements);
    delete[]databk;
}

void MergeSortREC(int*data,int *databk,int begin, int end)
{
    MergeT *stack,*tmp;
    int SSize,pointer,midpoint,I,A,B;
    SSize=100;
    stack=new MergeT[SSize];

    pointer=0;
    stack[pointer]._begin=begin;
    stack[pointer]._end=end;

    do
    {
#ifdef DEBUG
        cout <<endl;
        cout<<(begin=stack[pointer]._begin)<<" ";
        cout<<(end=stack[pointer]._end)<<" ";
#else
        begin=stack[pointer]._begin;
        end=stack[pointer]._end;
#endif
        pointer--;

        if(begin==-1)
        {
            begin=end;
#ifdef DEBUG
            cout<<(midpoint=stack[pointer]._begin)<<" ";
            cout<<(end=stack[pointer]._end)<<" ";
            cout<<"M0 ";
#else
            midpoint=stack[pointer]._begin;
            end=stack[pointer]._end;
#endif
            pointer--;
            I=0;
            A=begin;
            B=midpoint;
            do{databk[I++]=(data[A]<data[B])?data[A++]:data[B++];} while((A<midpoint)&&(B<end));
        
            if(A==midpoint)
            {
//              memcpy(databk+I,data+B,(end-B)*sizeof(int));
//              memcpy(data+begin,databk,(end-begin)*sizeof(int));
                memcpy(data+begin,databk,(B-begin)*sizeof(int));//same thing, I believe
            }
            else
            {
//              memcpy(databk+I,data+A,(midpoint-A)*sizeof(int));
//              memcpy(data+begin,databk,(end-begin)*sizeof(int));
                memcpy(data+end-(midpoint-A),data+A,(midpoint-A)*sizeof(int));
                memcpy(data+begin,databk,I*sizeof(int));
            }
        }
        else if (end-begin>2)
        {
            midpoint=begin+(end-begin>>1);
#ifdef DEBUG            
            cout<<"S1 ";
#endif            
            pointer++;
            stack[pointer]._begin=midpoint;
            stack[pointer]._end=end;
            
            pointer++;
            stack[pointer]._begin=-1;
            stack[pointer]._end=begin;
            
            pointer++;
            stack[pointer]._begin=begin;
            stack[pointer]._end=midpoint;
            
            pointer++;
            stack[pointer]._begin=midpoint;
            stack[pointer]._end=end;
        }
        else if ((end-begin==2)&&(data[begin]>data[begin+1])) Swap(data,begin,begin+1);

        if(pointer+3>=SSize)
        {
            tmp=stack;
            SSize+=100;
            stack=new MergeT[SSize];
#ifdef DEBUG            
            cout<<"S2!! ";
#endif            
            memcpy(stack,tmp,(pointer+1)*sizeof(MergeT));
            delete[]tmp;
        }
#ifdef DEBUG_PLUS
        getch();
#endif        
    }
    while (pointer>=0);
    delete[]stack;
}

void MemorySort(int*array,int length)
{
    int *TGV;
    int I;
    int nextnum;
    int cumulative;
    TGV = new int[MAX_VAL];
    for(I=0;I<MAX_VAL;I++)
       TGV[I]=0;
    for(I=0;I<length;I++)
        TGV[array[I]]++;
    cumulative=0;
    for(nextnum=0;nextnum<MAX_VAL;nextnum++)
        for(I=0;I<TGV[nextnum];I++)
        {
            array[cumulative]=nextnum;
            cumulative++;
//            if (cumulative>length) {cout<<"This is just so funny... <DIE>\n"<<nextnum; exit(1);}
        }
    if (cumulative!=length) cout<<"! - ";
}

