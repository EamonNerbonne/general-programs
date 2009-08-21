//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| Suffix tree (a.k.a. trie)                                                  |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "util/SuffixTree.h"
#include "util/for_each.hpp"
#include <stack>//TODO
#ifdef _MSC_VER
	#pragma warning(disable:4996) // deprecated
#endif

using namespace std;

//DECLARE_TYPEOF_COLLECTION(SuffixTreeNode*);

// ----------------------------------------------------------------------------- : Suffix tree

SuffixTree::SuffixTree()
	: end_here(0)
	, number(0)
{}
SuffixTree::SuffixTree(const std::string& filename)
	: end_here(0)
	, number(0)
{
	load(filename);
}

SuffixTree::~SuffixTree() {
	for(std::vector<SuffixTreeNode*>::iterator el = nodes.begin(); el!=nodes.end();el++)
	delete *el;
}

void SuffixTree::swap(SuffixTree& other) {
	std::swap(end_here, other.end_here);
	std::swap(number,   other.number);
	std::swap(nodes,    other.nodes);
}

// compare symbols inside SuffixTreeNodes
struct CompareSymbols {
	inline bool operator () (const SuffixTreeNode* a, const SuffixTreeNode* b) { return a->symbol < b->symbol; }
	inline bool operator () (const SuffixTreeNode* a, wchar_t               b) { return a->symbol < b;         }
	inline bool operator () (wchar_t               a, const SuffixTreeNode* b) { return a         < b->symbol; }
	inline bool operator () (wchar_t               a, wchar_t               b) { return a         < b;         }
};

SuffixTree* SuffixTree::lookup(wchar_t chr, bool insert) {
	std::vector<SuffixTreeNode*>::iterator pos = lower_bound(nodes.begin(), nodes.end(), chr, CompareSymbols());
	if (pos == nodes.end() || (**pos).symbol != chr) {
		if (insert) {
			SuffixTreeNode* new_node = new SuffixTreeNode(chr);
			nodes.insert(pos, new_node);
			return &new_node->tree;
		} else {
			return NULL;
		}
	} else {
		return &(**pos).tree;
	}
}

void SuffixTree::insert(const wchar_t* str, size_t copies) {
	if (copies == 0) return;
	SuffixTree* tr = this;
	while (str[0]) {
		tr->number += copies;
		tr = tr->lookup(str[0], true);
		str++;
	}
	tr->number   += copies;
	tr->end_here += copies;
}

size_t SuffixTree::lookup(const std::wstring& word) const {
	SuffixTree* tr = const_cast<SuffixTree*>(this);
	for (size_t i = 0 ; i < word.size() ; ++i) {
		tr = tr->lookup(word[i], false);
		if (!tr) return 0;
	}
	return tr->end_here;
}

// ----------------------------------------------------------------------------- : Iterator

wchar_t SuffixTree::iterator::symbol() const {
	return (**it).symbol;
}
SuffixTree* SuffixTree::iterator::child() const {
	return &(**it).tree;
}
float SuffixTree::iterator::probability() const {
	return (float) (**it).tree.number / parent->number;
}

// ----------------------------------------------------------------------------- : File IO


void SuffixTree::load(FILE* file) {
	wchar_t buf[1024];
	while (!feof(file)) {
		int copies = 1;
		if (fwscanf(file, L"%s *%d ", buf, &copies) && buf[0]) {
			insert(buf, copies);
		}
	}
}

void SuffixTree::save(FILE* file) {
	wchar_t buf[1024];
	save(file, buf, 0);
}
void SuffixTree::save(FILE* file, wchar_t* buf, int i) {
	if (end_here > 0) {
		buf[i] = 0;
		if (end_here == 1) {
			fwprintf(file, L"%s\n", buf);
		} else {
			fwprintf(file, L"%-20s *%d\n", buf, end_here);
		}
	}
	for (iterator it = begin() ; !it.end() ; ++it) {
		buf[i] = it.symbol();
		it.child()->save(file, buf, i + 1);
	}
}

void SuffixTree::load(const std::string& filename) {
	FILE* file = fopen(filename.c_str(), "rt");
	if (!file) return;
	load(file);
	fclose(file);
}

void SuffixTree::save(const std::string& filename) {
	FILE* file = fopen(filename.c_str(), "wt");
	if (!file) return;
	save(file);
	fclose(file);
}
