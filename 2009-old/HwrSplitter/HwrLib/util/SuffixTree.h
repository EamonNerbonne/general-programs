//+----------------------------------------------------------------------------+
//| Handwriting recognition - Utilities                                        |
//| Suffix tree (a.k.a. trie)                                                  |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

#ifndef SUFFIX_TREE_H
#define SUFFIX_TREE_H
#include "../stdafx.h"

// ----------------------------------------------------------------------------- : Suffix tree

struct SuffixTreeNode;

/// A suffix tree storing wstrings
/** Used as a dictionary */
class SuffixTree {
  public:
	SuffixTree();
	SuffixTree(const std::string& filename);
	~SuffixTree();
	
	/// Insert a single word, or multiple copies of it
	void insert(const wchar_t* str, size_t copies = 1);
	/// Read a dictionary from a file
	void load(FILE* file);
	void load(const std::string& filename);
	/// Save a dictionary to a file
	void save(FILE* file);
	void save(const std::string& filename);
	
	/// The total number of elments in the tree
	inline size_t size() { return number; }
	/// Probability that a word ends here
	inline float probability_end() { return (float)end_here / number; }
	/// The number of times a word occurs in the tree
	size_t lookup(const std::wstring& word) const;
	
	/// Iterator type
	struct iterator {
	  public:
		inline const iterator& operator++() { ++it; return *this; }
		inline bool operator == (const iterator& that) const { return it == that.it; }
		inline bool operator != (const iterator& that) const { return it != that.it; }
		inline bool end() const { return it == parent->nodes.end(); }
		/// Get the key for this child
		wchar_t     symbol() const;
		/// Get the actual child
		SuffixTree* child() const;
		/// Probability of this transition
		float       probability() const;
	  private:
		std::vector<SuffixTreeNode*>::const_iterator it;
		const SuffixTree* parent;
		friend class SuffixTree;
		inline iterator(std::vector<SuffixTreeNode*>::const_iterator it, const SuffixTree* parent)
			: it(it), parent(parent)
		{}
	};
	
	inline iterator begin() const { return iterator(nodes.begin(), this); }
	inline iterator end()   const { return iterator(nodes.end(),   this); }
	
	void swap(SuffixTree& other);
	
  private:
	/// Lookup up a certain key, optionally insert a child node if it is not there, otherwise returns NULL
	SuffixTree* lookup(wchar_t chr, bool insert);
	
	void save(FILE* file, wchar_t* buf, int i);
	
	size_t end_here; ///< Number of items that end here
	size_t number;   ///< Total number of values
	std::vector<SuffixTreeNode*> nodes;
};

struct SuffixTreeNode {
  public:
	SuffixTreeNode(wchar_t symbol) : symbol(symbol) {}
	
	wchar_t    symbol;
	SuffixTree tree;
};

// ----------------------------------------------------------------------------- : EOF
#endif
