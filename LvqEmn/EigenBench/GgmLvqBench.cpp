//by Eamon Nerbonne, 2011
//Compile this program with any of the following preprocessor flags defined; 
//e.g. g++ GgmLvqBench.cpp -std=c++0x  -DNDEBUG  -DLVQFLOAT=float -DLVQDIM=4 -O3 -march=native
//When run, the best timing of 10 runs is written to standard out, and 4 error rates for each of the 10 runs are written to standard error; the 4 error rates represent accuracies during training and should generally decrease.
//You can define EIGEN_DONT_VECTORIZE to disable eigen's vectorization
//You can define NO_BOOST to use the mt19937 implementation provided by the compiler and not boost's, however the generated dataset may differ and this may (very slightly) affect performance
//You can define LVQFLOAT as float or double; computations will use that type; (default: double)
//You can define LVQDIM as some fixed number in range [2..19] (default:2) which controls the number of dimensions the algorithm will work in.  Other positive numbers might work too.


//#define LVQDYNAMIC
#ifndef LVQDIM
#define LVQDIM 2
#endif
#ifndef LVQFLOAT
#define LVQFLOAT double
#endif



#ifdef _MSC_VER
#pragma warning (disable: 4714)
#pragma warning( push, 3 )
#pragma warning (disable: 4242)
#endif

#include <bench/BenchTimer.h>
#include <fstream>
#include <iostream>
#include <typeinfo>

#define EIGEN_VECTORIZE_SSE3
#define EIGEN_VECTORIZE_SSSE3
#define EIGEN_VECTORIZE_SSE4_1

#include <Eigen/Core>
#include <Eigen/Eigenvalues>
#include <Eigen/StdVector>
#include <vector>
#include <algorithm>
#include <numeric>
#include <random>

typedef std::mt19937 mtGen;
typedef std::normal_distribution<double> normDistrib;
typedef std::uniform_real_distribution<double> uniformDistrib;

#ifdef _MSC_VER
#pragma warning( pop)
#pragma warning (disable: 4514)
#endif

using namespace Eigen;
using namespace std;

#ifdef LVQDYNAMIC
#define LVQDIM_DECL Dynamic
#else
#define LVQDIM_DECL LVQDIM
#endif


#define str(s) #s
#define LVQ_BENCH_SETTINGS_HELPER(dimdecl,dim, type) (str(dim) "(" str(dimdecl) ") dimensions of " str(type) "s")
#define VERSIONSTR_BENCHSETTINGS LVQ_BENCH_SETTINGS_HELPER(LVQDIM_DECL, LVQDIM, LVQFLOAT)
#ifdef EIGEN_DONT_VECTORIZE
#define VERSIONSTR_VECTORIZATION "NV"
#else
#define VERSIONSTR_VECTORIZATION "V"
#endif
#ifndef NDEBUG
#define VERSIONSTR_DEBUG "[DEBUG]"
#else
#define VERSIONSTR_DEBUG ""
#endif
#ifdef _MSC_VER
#define VERSIONSTR_COMPILER "MSC"
#else
#ifdef __GNUC__
#define VERSIONSTR_COMPILER "GCC"
#else
#define VERSIONSTR_COMPILER "???"
#endif
#endif


#define LVQ_ITERFACTOR_PERPROTO (LVQFLOAT(1.0/4000.0))

#define LR0 (LVQFLOAT(0.03))
#define LrScaleP (LVQFLOAT(0.03))
#define LrScaleB (LVQFLOAT(0.3))

#define MEANSEP (LVQFLOAT(2.0))

#define DIMS 19
#define EPOCHS 3
#define CLASSCOUNT 4
#define PROTOSPERCLASS 3
#define BENCH_RUNS (10)
#ifdef NDEBUG
#define POINTS_PER_CLASS 40000
#else
#define POINTS_PER_CLASS 300
#endif

typedef Matrix<LVQFLOAT, LVQDIM_DECL, Dynamic> Matrix_LN;
typedef Matrix_LN Matrix_P;
typedef Matrix<LVQFLOAT, Dynamic, Dynamic> Matrix_NN;
typedef Matrix<LVQFLOAT, LVQDIM_DECL, LVQDIM_DECL> Matrix_LL;
typedef Matrix<LVQFLOAT, Dynamic, 1> Vector_N;
typedef Matrix<LVQFLOAT, LVQDIM_DECL, 1> Vector_L;
typedef Matrix<unsigned char, Dynamic, Dynamic, RowMajor> ClassDiagramT;
typedef Map<Vector_N, Aligned> MVectorXd;

struct MatchQuality {
    LVQFLOAT distGood, distBad;
    LVQFLOAT costFunc;
    LVQFLOAT mu;
    LVQFLOAT lr;
    bool isErr;
};

static LVQFLOAT sqr(LVQFLOAT v) { return v * v; }

struct GoodBadMatch {
    LVQFLOAT distGood, distBad;
    int matchGood, matchBad;
    inline GoodBadMatch()
        : distGood(std::numeric_limits<LVQFLOAT>::infinity())
        , distBad(std::numeric_limits<LVQFLOAT>::infinity())
    {}

    EIGEN_STRONG_INLINE MatchQuality GgmQuality() {
        MatchQuality retval;
        retval.isErr = distGood >= distBad;

        retval.costFunc = tanh((distGood - distBad) / (LVQFLOAT)4.0);
        retval.distBad = distBad;
        retval.distGood = distGood;
        retval.mu = (LVQFLOAT)(1.0 / 4.0) * (1 - sqr(retval.costFunc));
        return retval;
    }
};

template <typename T> EIGEN_STRONG_INLINE static LVQFLOAT projectionSquareNorm(T const& projectionMatrix) {
    auto projectionSquareExpression = projectionMatrix.transpose() * projectionMatrix;
    return projectionSquareExpression.diagonal().sum();
}

EIGEN_STRONG_INLINE static LVQFLOAT normalizeProjection(Matrix_P& projectionMatrix) {
    LVQFLOAT scale = LVQFLOAT(LVQFLOAT(1.0) / sqrt(projectionSquareNorm(projectionMatrix)));
    projectionMatrix *= scale;
    return scale;
}

static Matrix_NN ComputeClassMeans(int classCount, Matrix_NN const& points, VectorXi const& labels) {
    Matrix_NN means = Matrix_NN::Zero(points.rows(), classCount);
    VectorXi freq = VectorXi::Zero(classCount);

    for (ptrdiff_t i = 0;i < points.cols();++i) {
        means.col(labels(i)) += points.col(i);
        freq(labels(i))++;
    }
    for (int i = 0;i < classCount;i++) {
        if (freq(i) > 0)
            means.col(i) /= LVQFLOAT(freq(i));
    }
    return means;
}

static pair<Matrix_NN, VectorXi> InitByClassMeans(int classCount, int prototypesPerClass, Matrix_NN const& points, VectorXi const& labels) {
    Matrix_NN  prototypes(points.rows(), prototypesPerClass * classCount);
    VectorXi protolabels(prototypes.cols());
    Matrix_NN classmeans = ComputeClassMeans(classCount, points, labels);
    int pi = 0;
    for (int i = 0; i < classCount; ++i) {
        for (int subpi = 0; subpi < prototypesPerClass; ++subpi, ++pi) {
            prototypes.col(pi).noalias() = classmeans.col(i);
            protolabels(pi) = (int)i;
        }
    }

    return make_pair(prototypes, protolabels);
}


template <typename TPoints>
struct PrincipalComponentAnalysisTemplate {

    typedef Eigen::Matrix<typename TPoints::Scalar, TPoints::RowsAtCompileTime, 1> TPoint;
    typedef Eigen::Matrix<typename TPoints::Scalar, TPoints::RowsAtCompileTime, TPoints::RowsAtCompileTime> TMatrix;
    typedef typename TPoints::Scalar Scalar;
    typedef typename TPoints::Index Index;

    static void DoPcaFromCov(TMatrix const& covarianceMatrix, TMatrix& transform, TPoint& eigenvalues) {
        using namespace std;

        Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, Eigen::ComputeEigenvectors);
        TPoint eigenvaluesUnsorted = eigenSolver.eigenvalues();
        TMatrix eigVecUnsorted = eigenSolver.eigenvectors();

        vector<pair<Scalar, Index> > v;
        for (Index i = 0;i < eigenvaluesUnsorted.size();++i)
            v.push_back(make_pair(-eigenvaluesUnsorted(i), i));
        sort(v.begin(), v.end());

        assert(eigVecUnsorted.cols() == eigVecUnsorted.rows());
        transform.resize(eigVecUnsorted.cols(), eigVecUnsorted.rows());
        eigenvalues.resize(eigenvaluesUnsorted.size());

        for (ptrdiff_t i = 0;i < eigenvalues.size();++i) {
            transform.row(i).noalias() = eigVecUnsorted.col(v[(size_t)i].second);
            eigenvalues(i) = eigenvaluesUnsorted(v[(size_t)i].second);
        }
        //now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
        //eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
        //and then we could transform the data too ...
    }
};


typedef PrincipalComponentAnalysisTemplate<Matrix_NN> PcaHighDim;
typedef PrincipalComponentAnalysisTemplate<Matrix_P> PcaLowDim;


inline static Matrix_LL CovarianceL(Matrix_LN const& points) { // faster for small matrices
    Vector_L mean = points.rowwise().mean();
    Vector_L diff = Vector_L::Zero(points.rows());
    Matrix_LL cov = Matrix_LL::Zero(points.rows(), points.rows());
    for (int i = 0;i < points.cols();++i) {
        diff.noalias() = points.col(i) - mean;
        cov.noalias() += diff * diff.transpose();
    }
    return cov * (LVQFLOAT)(1.0 / (points.cols() - 1.0));
}

inline static Matrix_NN CovarianceN(Matrix_NN const& points) { // faster for large matrices
    Vector_N mean = points.rowwise().mean();
    Matrix_NN cov = Matrix_NN::Zero(points.rows(), points.rows());
    Matrix_NN meanCentered = points.colwise() - mean;
    cov.selfadjointView<Eigen::Lower>().rankUpdate(meanCentered, (LVQFLOAT)(1.0 / (points.cols() - 1.0)));
    cov.triangularView<Eigen::StrictlyUpper>() = cov.adjoint();
    return cov;
}


static Matrix_LL normalizingB(Matrix_LL const& cov) {
    Vector_L eigVal;
    Matrix_LL pcaLowD;
    PcaLowDim::DoPcaFromCov(cov, pcaLowD, eigVal);
    return eigVal.array().sqrt().inverse().matrix().asDiagonal() * pcaLowD;
}


inline Matrix_P PcaProjectIntoLd(Matrix_NN const& points, ptrdiff_t lowDimCount) {
    Matrix_NN transform;
    Vector_N eigenvalues;
    PcaHighDim::DoPcaFromCov(CovarianceN(points), transform, eigenvalues);
    return transform.topRows(lowDimCount);
}

class GgmLvqModel
{
    LVQFLOAT trainIter;
    LVQFLOAT iterationScaleFactor;
    int epochsTrained;
    int classCount;

    Matrix_P P;

    Matrix_NN prototypes;
    Matrix_P P_prototypes;
    mutable Matrix_P vDelta;
    VectorXi prototype_labels;
    Vector_N bias;
    mutable Vector_N accum;
    //protoList prototype;

    vector<Matrix_P> B_transposed_by_rows;

    //we will preallocate a few temp vectors to reduce malloc/free overhead.
    Vector_N m_vJ, m_vK;
    Matrix_P m_PpseudoinvT;

    EIGEN_STRONG_INLINE void ComputePP() {
        P_prototypes.noalias() = P * prototypes;
    }

    EIGEN_STRONG_INLINE Matrix_LL GetB(ptrdiff_t i) {
        Matrix_LL B(LVQDIM, LVQDIM);
        for (ptrdiff_t row = 0; row < LVQDIM;row++) {
            B.row(row).noalias() = B_transposed_by_rows[row].col(i).transpose();
        }
        return B;
    }
    EIGEN_STRONG_INLINE void SetB(ptrdiff_t i, Matrix_LL B) {
        for (ptrdiff_t row = 0; row < LVQDIM;row++) {
            B_transposed_by_rows[row].col(i).noalias() = B.row(row).transpose();
        }
        bias(i) = -log(sqr(B.determinant()));
    }

    EIGEN_STRONG_INLINE void RecomputeBias(ptrdiff_t i) {
        Matrix_LL B(GetB(i));
        bias(i) = -log(sqr(B.determinant()));
        assert(bias(i) == bias(i));//notnan
    }


    LVQFLOAT stepLearningRate() { //starts at 1.0, descending with power -0.75
        LVQFLOAT scaledIter = trainIter * iterationScaleFactor + (LVQFLOAT)1.0;
        ++trainIter;
        return (LVQFLOAT)1.0 / sqrt(scaledIter * sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) 
    }
public:
    GgmLvqModel(int classCount, int protosPerClass, Matrix_NN const& points, VectorXi const& labels)
        : trainIter(0)
        , epochsTrained(0)
        , classCount(classCount)
        , prototypes(points.rows(), classCount* protosPerClass)
        , P_prototypes(LVQDIM, classCount* protosPerClass)
        , vDelta(LVQDIM, classCount* protosPerClass)
        , prototype_labels(classCount* protosPerClass)
        , bias(classCount* protosPerClass)
        , accum(classCount* protosPerClass)
        , B_transposed_by_rows(LVQDIM)
        , m_vJ(points.rows())
        , m_vK(points.rows())
        , m_PpseudoinvT(LVQDIM, points.rows())
    {
        iterationScaleFactor = LVQ_ITERFACTOR_PERPROTO / sqrt((LVQFLOAT)protosPerClass * classCount);

        P = PcaProjectIntoLd(points, LVQDIM);
        normalizeProjection(P);

        auto protos = InitByClassMeans(classCount, protosPerClass, points, labels);
        prototypes = get<0>(protos);
        prototype_labels = get<1>(protos);

        Matrix_LL initB = normalizingB(CovarianceL(P * points));
        for (ptrdiff_t row = 0; row < LVQDIM;row++) {
            B_transposed_by_rows[row] = Matrix_P(LVQDIM, prototypes.cols());
            B_transposed_by_rows[row].colwise() = initB.row(row).transpose();
        }

        for (ptrdiff_t protoIndex = 0; protoIndex < prototype_labels.size(); ++protoIndex) {
            RecomputeBias(protoIndex);
        }

        ComputePP();
    }

    void ClassBoundaryDiagram(LVQFLOAT x0, LVQFLOAT x1, LVQFLOAT y0, LVQFLOAT y1, ClassDiagramT& classDiagram) const {
        int cols = static_cast<int>(classDiagram.cols());
        int rows = static_cast<int>(classDiagram.rows());
        LVQFLOAT xDelta = (x1 - x0) / cols;
        LVQFLOAT yDelta = (y1 - y0) / rows;
        LVQFLOAT xBase = x0 + xDelta * (LVQFLOAT)0.5;
        LVQFLOAT yBase = y0 + yDelta * (LVQFLOAT)0.5;
        typedef Matrix<LVQFLOAT, 2, 1> Vector_2;

        Matrix_LN B_diff_x0_y(LVQDIM, prototype_labels.size()) //Contains B_i * (testPoint[x, y0] - P*proto_i)  for all proto's i
                                                                                                    //will update to include changes to X.
            , B_xDelta(LVQDIM, prototype_labels.size())//Contains B_i * (xDelta, 0)  for all proto's i
            , B_yDelta(LVQDIM, prototype_labels.size());//Contains B_i * (0 , yDelta)  for all proto's i

        for (ptrdiff_t pi = 0; pi < (ptrdiff_t)prototype_labels.size(); ++pi) {
            Matrix_LN smallB(LVQDIM, 2);
            for (ptrdiff_t row = 0; row < LVQDIM;row++) {
                smallB.row(row) = B_transposed_by_rows[row].col(pi).transpose().leftCols<2>();
            }
            B_diff_x0_y.col(pi).noalias() = smallB * (Vector_2(xBase, yBase) - P_prototypes.col(pi).topRows<2>());
            B_xDelta.col(pi).noalias() = smallB * Vector_2(xDelta, 0.0);
            B_yDelta.col(pi).noalias() = smallB * Vector_2(0.0, yDelta);
        }

        for (int yRow = 0; yRow < rows; yRow++) {
            Matrix_P B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
            for (int xCol = 0; xCol < cols; xCol++) {
                // x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
                Matrix_NN::Index bestProtoI;
                (B_diff_x_y.colwise().squaredNorm() + bias.transpose()).minCoeff(&bestProtoI);
                classDiagram(yRow, xCol) = (unsigned char)prototype_labels((ptrdiff_t)bestProtoI);

                B_diff_x_y.noalias() += B_xDelta;
            }
            B_diff_x0_y.noalias() += B_yDelta;
        }
    }


    //inline LVQFLOAT SqrDistanceTo(size_t protoIndex, Vector_L const & P_otherPoint) const { return P_prototypes.col(protoIndex).SqrDistanceTo(P_otherPoint); }

    //end for templates

    EIGEN_STRONG_INLINE GoodBadMatch findMatches(Vector_L const& trainPoint, int trainLabel) const {
        GoodBadMatch match;
        assert(trainPoint.sum() == trainPoint.sum());//no NaN
        vDelta.noalias() = P_prototypes.colwise() - trainPoint;
        accum.noalias() = bias + (B_transposed_by_rows[0].array() * vDelta.array()).colwise().sum().square().transpose().matrix();
        for (ptrdiff_t i = 1;i < LVQDIM;i++) {
            accum.noalias() += (B_transposed_by_rows[i].array() * vDelta.array()).colwise().sum().square().transpose().matrix();
        }

        for (ptrdiff_t i = 0;i < prototypes.cols();i++) {
            LVQFLOAT curDist = accum(i);
            if (prototype_labels(i) == trainLabel) {
                if (curDist < match.distGood) {
                    match.matchGood = (int)i;
                    match.distGood = curDist;
                }
            }
            else {
                if (curDist < match.distBad) {
                    match.matchBad = (int)i;
                    match.distBad = curDist;
                }
            }
        }
        return match;
    }

    MatchQuality ComputeMatches(Vector_N const& unknownPoint, int pointLabel) const { return this->findMatches(P * unknownPoint, pointLabel).GgmQuality(); }
    MatchQuality learnFrom(Vector_N const& trainPoint, int trainLabel) {
        using namespace std;
        LVQFLOAT learningRate = stepLearningRate();


        LVQFLOAT lr_point = -LR0 * learningRate,
            lr_P = lr_point * LrScaleP,
            lr_B = lr_point * LrScaleB,// * (1.0 - learningRate),
            lr_bad = sqr((LVQFLOAT)1.0 - learningRate);

        assert(lr_P <= 0 && lr_B <= 0 && lr_point <= 0);

        Vector_L P_trainPoint(P * trainPoint);

        GoodBadMatch matches = findMatches(P_trainPoint, trainLabel);

        //now matches.good is "J" and matches.bad is "K".
        ptrdiff_t Ji = matches.matchGood;
        ptrdiff_t Ki = matches.matchBad;
        MatchQuality ggmQuality = matches.GgmQuality();
        LVQFLOAT muJ2 = 2 * ggmQuality.mu;
        LVQFLOAT muK2 = -muJ2;

        Matrix_LL BJ = GetB(Ji),
            BK = GetB(Ki);

        MVectorXd vJ(m_vJ.data(), m_vJ.size());
        MVectorXd vK(m_vK.data(), m_vK.size());

        Vector_L P_vJ = P_prototypes.col(Ji) - P_trainPoint;
        Vector_L P_vK = P_prototypes.col(Ki) - P_trainPoint;
        Vector_L muJ2_Bj_P_vJ = muJ2 * (BJ * P_vJ);
        Vector_L muK2_Bk_P_vK = muK2 * (BK * P_vK);
        vJ = prototypes.col(Ji) - trainPoint;
        vK = prototypes.col(Ki) - trainPoint;
        Vector_L muJ2_BjT_Bj_P_vJ = BJ.transpose() * muJ2_Bj_P_vJ;
        Vector_L muK2_BkT_Bk_P_vK = BK.transpose() * muK2_Bk_P_vK;

        Matrix_LL neg_muJ2_JBinvT = -muJ2 * BJ.inverse().transpose();
        Matrix_LL neg_muK2_KBinvT = -muK2 * BK.inverse().transpose();

        BJ.noalias() += lr_B * (muJ2_Bj_P_vJ * P_vJ.transpose() + neg_muJ2_JBinvT);
        BK.noalias() += (lr_bad * lr_B) * (muK2_Bk_P_vK * P_vK.transpose() + neg_muK2_KBinvT);
        SetB(Ji, BJ);
        SetB(Ki, BK);

        prototypes.col(Ji).noalias() += P.transpose() * (lr_point * muJ2_BjT_Bj_P_vJ);
        prototypes.col(Ki).noalias() += P.transpose() * (lr_bad * lr_point * muK2_BkT_Bk_P_vK);


        P.noalias() += (lr_P * muK2_BkT_Bk_P_vK) * vK.transpose() + (lr_P * muJ2_BjT_Bj_P_vJ) * vJ.transpose();
        normalizeProjection(P);

        ComputePP();

        return ggmQuality;
    }

    Matrix_LN const& GetProjectedPrototypes() const {
        return P_prototypes;
    }

    vector<int> GetPrototypeLabels() const;
    Matrix_P const& Projection() const { return P; }
};


class LvqDatasetStats {
    LVQFLOAT meanCost_sum, errorRate_sum, muMean_sum, muMax_val;
    size_t counter;
public:
    LvqDatasetStats() : meanCost_sum(0.0), errorRate_sum(0.0), muMean_sum(0.0), muMax_val(0.0), counter(0) {    }
    void Add(MatchQuality match) {
        assert(-1 <= match.costFunc && match.costFunc <= 1);
        counter++;
        errorRate_sum += match.isErr ? 1 : 0;
        meanCost_sum += match.costFunc;
        muMean_sum += match.mu;
        muMax_val = max(muMax_val, match.mu);
    }

    LVQFLOAT meanCost() const { return meanCost_sum / counter; }
    LVQFLOAT errorRate()  const { return errorRate_sum / counter; }
    LVQFLOAT muMean()  const { return muMean_sum / counter; }
    LVQFLOAT muMax() const { return muMax_val; }
};


static LvqDatasetStats ComputeCostAndErrorRate(GgmLvqModel const& model, Matrix_NN const& points, VectorXi const& labels) {
    LvqDatasetStats stats;
    Vector_N point;
    for (int i = 0;i < points.cols();++i) {
        point = points.col(i);
        MatchQuality matchQ = model.ComputeMatches(point, labels(i));

        stats.Add(matchQ);
    }
    return stats;
}


static void PrintModelStatus(char const* label, GgmLvqModel const& model, Matrix_NN const& points, VectorXi const& labels) {
    using namespace std;

    auto stats = ComputeCostAndErrorRate(model, points, labels);

    cerr << label << " err: " << stats.errorRate() << ", cost: " << stats.meanCost();

    Matrix_LN protos = model.GetProjectedPrototypes();

    Vector_L minV = protos.rowwise().minCoeff();
    Vector_L maxV = protos.rowwise().maxCoeff();
    Vector_L range = maxV - minV;
    minV -= range;
    maxV += range;

    ClassDiagramT diagram(500, 500);
    model.ClassBoundaryDiagram(minV(0), maxV(0), minV(1), maxV(1), diagram);
    Matrix_P projMatrix = model.Projection();

    cerr << " ignore:" << diagram.cast<unsigned>().sum() << ";";
    cerr << endl;
}

static void EIGEN_STRONG_INLINE prefetch(void const* start, size_t lines) {
    for (size_t i = 0;i < lines;i++)
        _mm_prefetch((const char*)start + 64 * i, _MM_HINT_T0);//_MM_HINT_T0 or _MM_HINT_NTA
}

static void TrainModel(mtGen& shuffleRand, Matrix_NN const& points, VectorXi const& labels, int epochs, GgmLvqModel& model) {
    int dims = static_cast<int>(points.rows());
    Vector_N pointA(dims);
    size_t cacheLines = (dims * sizeof(points(0, 0)) + 63) / 64;

    vector<int> shuffledOrder((size_t)points.cols());
    for (size_t i = 0;i < (size_t)points.cols();++i)
        shuffledOrder[i] = (int)i;
    for (int epoch = 0; epoch < epochs; ++epoch) {
        shuffle(shuffledOrder.begin(), shuffledOrder.end(), shuffleRand);
        for (size_t tI = 0; tI < shuffledOrder.size(); ++tI) {
            int pointIndex = shuffledOrder[tI];
            int pointClass = labels(pointIndex);
            pointA = points.col(pointIndex);
            prefetch(&points.coeff(0, shuffledOrder[(tI + 1) % shuffledOrder.size()]), cacheLines);
            model.learnFrom(pointA, pointClass);
        }
    }
}


static void TestModel(mtGen& shuffleRand, Matrix_NN const& points, VectorXi const& labels, int protosPerClass, int iters) {
    BenchTimer t;
    t.start();
    GgmLvqModel model(labels.maxCoeff() + 1, protosPerClass, points, labels);
    t.stop();

    cerr << "constructing with " << VERSIONSTR_BENCHSETTINGS << "(" << VERSIONSTR_VECTORIZATION << "): " << t.value() << "s\n";

    PrintModelStatus("Initial", model, points, labels);

    t.start();
    int num_groups = 3;
    for (int i = 0;i < num_groups;i++) {
        int itersDone = iters * i / num_groups;
        int itersUpto = iters * (i + 1) / num_groups;
        int itersTodo = itersUpto - itersDone;
        if (itersTodo > 0) {
            TrainModel(shuffleRand, points, labels, itersTodo, model);
            PrintModelStatus("Trained", model, points, labels);
        }
    }
    t.stop();
    cerr << "training: " << t.value() << "s\n\n";
}

//randomizes all values of the matrix; each is independently drawn from a normal distribution with provided mean and sigma (=stddev).
template<typename T> static void randomMatrixInit(mtGen& rng, Eigen::MatrixBase< T>& mat, LVQFLOAT mean, LVQFLOAT sigma) {
    normDistrib distrib(mean, sigma);
    auto rndGen = std::bind(distrib, rng);

    for (int j = 0; j < mat.cols(); j++)
        for (int i = 0; i < mat.rows(); i++)
            mat(i, j) = (LVQFLOAT)rndGen();
}

template <typename T> static T randomUnscalingMatrix(mtGen& rngParams, int dims) {
    T P(dims, dims);
    LVQFLOAT Pdet = 0.0;
    while (!(Pdet > 0.1 && Pdet < 10)) {
        randomMatrixInit(rngParams, P, 0, 1.0);
        Pdet = P.determinant();
        assert(Pdet != 0);
        if (fabs(Pdet) <= std::numeric_limits<LVQFLOAT>::epsilon()) continue;//exceedingly unlikely.

        if (Pdet < 0.0) //sign doesn't _really_ matter.
            P.col(0) *= -1;
        LVQFLOAT scale = pow(fabs(Pdet), (LVQFLOAT)-1.0 / LVQFLOAT(dims));
        assert(scale == scale);

        P *= scale;
        Pdet = P.determinant();
    }
    return P;
}


template <typename T> static T randomScalingMatrix(mtGen& rngParams, int dims, LVQFLOAT detScalePower) {
    uniformDistrib distrib;
    auto rndGen = std::bind(distrib, rngParams);

    T P = randomUnscalingMatrix<T>(rngParams, dims);
    P *= (LVQFLOAT)exp(rndGen() * 2.0 * detScalePower - detScalePower);
    return P;
}

static Matrix_NN MakePointCloud(mtGen& rngParams, mtGen& rngInst, int dims, int pointCount, LVQFLOAT meansep) {

    Vector_N offset(dims);
    randomMatrixInit(rngParams, offset, 0, meansep / sqrt(static_cast<LVQFLOAT>(dims)));

    Matrix_NN P = randomScalingMatrix<Matrix_NN>(rngParams, dims, 1.0);
    Matrix_NN points(dims, pointCount);
    randomMatrixInit(rngInst, points, 0, 1.0);

    return P * points + offset * Vector_N::Ones(pointCount).transpose();
}

static pair<Matrix_NN, VectorXi> ConstructGaussianClouds(mtGen& rngParams, mtGen& rngInst, int dims, int classCount, int pointsPerClass, LVQFLOAT meansep) {
    Matrix_NN allpoints(dims, classCount * pointsPerClass);
    for (int classLabel = 0;classLabel < classCount; classLabel++)
        allpoints.block(0, classLabel * pointsPerClass, dims, pointsPerClass) = MakePointCloud(rngParams, rngInst, dims, pointsPerClass, meansep * classCount);

    VectorXi trainingLabels = VectorXi::Zero(allpoints.cols());
    for (int i = 0; i < (int)trainingLabels.size(); ++i)
        trainingLabels(i) = i / pointsPerClass;

    return make_pair(allpoints, trainingLabels);
}

static double EasyLvqTest() {
    mtGen rngP(73), rngI(37), rngS(42);
    BenchTimer t;
    auto dataset = ConstructGaussianClouds(rngP, rngI, DIMS, CLASSCOUNT, POINTS_PER_CLASS, MEANSEP);

    for (int bI = 0;bI < BENCH_RUNS;++bI)
    {
        t.start();
        TestModel(rngS, get<0>(dataset), get<1>(dataset), PROTOSPERCLASS, EPOCHS);
        t.stop();
    }
    return t.best();
}

static size_t fileSize(const char* filepath) {
    ifstream f(filepath, ifstream::binary | ifstream::in);
    if (!f.good())  return 0;//e.g. if you call in windows without ".exe" we won't actually _find_ the executable; return 0 then.
    f.seekg(0, ios_base::end);
    return f.tellg() - ifstream::pos_type();
}


int main(int, char* argv[]) {
    double seconds = EasyLvqTest();
    cout.precision(3);
    cout << "EigenBench" << VERSIONSTR_VECTORIZATION << VERSIONSTR_DEBUG << " on " << VERSIONSTR_COMPILER << " with " << VERSIONSTR_BENCHSETTINGS << ": " << seconds << "s " << fileSize(argv[0]) / 1024 << "KB\n"; //resizeTest() <<"s; "
    return 0;
}
