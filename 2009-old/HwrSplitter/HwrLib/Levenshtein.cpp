#include "stdafx.h"
#include "util/Levenshtein.h"
//Levenshtein distance by Anders Sewerin Johansen.
// From: http://www.merriampark.com/ld.htm#CPLUSPLUS
// Code: http://www.merriampark.com/ldcpp.htm
//


namespace levenshtein {

int distance(const std::wstring source, const std::wstring target) {

  // Step 1

  const int n = (int)source.length();
  const int m = (int)target.length();
  if (n == 0) {
    return m;
  }
  if (m == 0) {
    return n;
  }

  // Good form to declare a TYPEDEF

  typedef std::vector< std::vector<int> > Tmatrix; 

  Tmatrix matrix(n+1);

  // Size the vectors in the 2.nd dimension. Unfortunately C++ doesn't
  // allow for allocation on declaration of 2.nd dimension of vec of vec

  for (int i = 0; i <= n; i++) {
    matrix[i].resize(m+1);
  }

  // Step 2

  for (int i = 0; i <= n; i++) {
    matrix[i][0]=i;
  }

  for (int j = 0; j <= m; j++) {
    matrix[0][j]=j;
  }

  // Step 3

  for (int i = 1; i <= n; i++) {

    const wchar_t s_i = source[i-1];

    // Step 4

    for (int j = 1; j <= m; j++) {

      const wchar_t t_j = target[j-1];

      // Step 5

      int cost;
      if (s_i == t_j) {
        cost = 0;
      }
      else {
        cost = 1;
      }

      // Step 6

      const int above = matrix[i-1][j];
      const int left = matrix[i][j-1];
      const int diag = matrix[i-1][j-1];
	  const int cell = std::min( above + 1, std::min(left + 1, diag + cost));


      matrix[i][j]=cell;
    }
  }

  // Step 7

  return matrix[n][m];
}
}
