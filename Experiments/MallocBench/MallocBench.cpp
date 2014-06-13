#include<iostream>
#include <bench\BenchTimer.h>

#define ITERS 1000000
void main() {

	Eigen::BenchTimer t;
	for(int j=0;j<10;j++) {
		t.start();
		for(int i=0;i<ITERS;i++) {
			free(malloc(128));
		}
		t.stop();
	}
	std::cout <<" Best:" << t.best()<<"s;"<<t.worst()<< "s for "<<ITERS<<" iters\n";
}
