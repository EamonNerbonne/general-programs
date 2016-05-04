#include<iostream>
#include <bench\BenchTimer.h>

#define ITERS 10000000
#define clocks_per_s 4000000000.0
void main() {

	Eigen::BenchTimer t;
	for(int j=0;j<10;j++) {
		t.start();
		for(int i=0;i<ITERS;i++) {
			free(malloc(1));
		}
		t.stop();
	}
	std::cout <<" Best:" << t.best()/ ITERS*clocks_per_s <<"s;"<<t.worst()/ITERS*clocks_per_s << "s for "<<ITERS<<" iters\n";
}
