#include "StdAfx.h"
#include "NearestNeighbor.h"
#include "PCA.h"
#include "utils.h"

using namespace Eigen;
using std::sort;

void NearestNeighbor::init() {
    for (size_t i = 0;i < idxs.size();i++)
        idxs[i] = (unsigned)i;
    Vector_2 eigenvalues;
    PcaLowDim::DoPca(sortedPoints, this->transform, eigenvalues);
    sortedPoints = transform * sortedPoints;
    sort(idxs.begin(), idxs.end(), [this](unsigned a, unsigned b) ->bool {return sortedPoints(0, a) < sortedPoints(0, b);});
}

int NearestNeighbor::nearestIdx(Vector_2 const& point) const
{
    //we transform the point into the PCA-d space.
    //then binary search to find nearest point in primary dim
    //then look backward and forward until distance to BEST < primary-dim-only distance to CURRENT.
    Vector_2 Ppoint = transform * point;

    unsigned rStart = 0, rEnd = (unsigned)idxs.size();
    while (rStart + 1 < rEnd) {
        unsigned mid = (rStart + rEnd) / 2;
        if (Ppoint(0) < sortedPoints(0, idxs[mid]))
            rEnd = mid;
        else
            rStart = mid;
    }
    auto primDimSqDist = [this, &Ppoint](unsigned i) -> double {return sqr(sortedPoints(0, idxs[i]) - Ppoint(0)); };
    auto sqDist = [this, &Ppoint](unsigned i) ->double { return  (sortedPoints.col(idxs[i]) - Ppoint).squaredNorm();};
    unsigned bestI = rStart;
    double bestSqDist = sqDist(bestI);
    for (unsigned forwardI = rStart + 1;forwardI < idxs.size() && primDimSqDist(forwardI) < bestSqDist; forwardI++) {
        double currSqDist = sqDist(forwardI);
        if (currSqDist < bestSqDist) {
            bestI = forwardI;
            bestSqDist = currSqDist;
        }
    }
    for (int reverseI = rStart - 1;reverseI >= 0 && primDimSqDist(reverseI) < bestSqDist; reverseI--) {
        double currSqDist = sqDist(reverseI);
        if (currSqDist < bestSqDist) {
            bestI = reverseI;
            bestSqDist = currSqDist;
        }
    }
    return idxs[bestI];
}
