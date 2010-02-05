//#define DIMS 16
//#define ITERS 1000000
//USING_PART_OF_NAMESPACE_EIGEN
//
//MatrixXd DoTest(MatrixXd A, MatrixXd C) {
//	MatrixXd B,D;
//	B.setIdentity(DIMS,DIMS);
//	D.setIdentity(DIMS,DIMS);
//
//	MatrixXd tmp;
//
//	progress_timer t;
//	for(int i=0;i<ITERS;i++) {
//		//*
//		D = (C + (A*B).lazy()).lazy();
//		B = (C + (A*D).lazy()).lazy();
//		/*/
//		tmp=(A*B).lazy();
//		D = (C + tmp).lazy();
//		tmp=(A*D).lazy();
//		B = (C + tmp).lazy();
//		/**/
//	}
//	return B;
//}
//
//VectorXd DoTest2(MatrixXd A, MatrixXd C) {
//	VectorXd B,D;
//	VectorXd C0 = C.col(0);
//	B.setOnes(DIMS);
//	D.setOnes(DIMS);
//
//	VectorXd tmp;
//
//	progress_timer t;
//	for(int i=0;i<ITERS;i++) {
//		
//		//*
//		D = (C0 + (A*B).eval()).lazy();
//		B = (C0 + (A*D).eval()).lazy();
//		/*/
//		tmp = (A*B).lazy();
//		D = C0 + tmp;
//		tmp = (A*D).lazy();
//		B = C0 + tmp;
//		/**/
//	}
//	return B;
//}
//
//
//void mytest(){
//	MatrixXd trans;
//	trans.setIdentity(DIMS,DIMS);
//
//	MatrixXd add;
//	add.setZero(DIMS,DIMS);
//
//	MatrixXd ignore = DoTest2(trans,add);
//
//	std::cout<<ignore.sum()<<std::endl;
//}
