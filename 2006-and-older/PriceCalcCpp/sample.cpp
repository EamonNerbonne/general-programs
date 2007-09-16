#include <boost/spirit/core.hpp>
#include <iostream>
#include <string>
#include <hash_map>
#include <typeinfo>
using namespace std;
template <typename T> basic_string<T> myGetline(basic_istream<T,char_traits<T> > &from) {
	T next,terminate=from.widen('\n');
	basic_string<T> line;
	for(from.get(next);next!= terminate&&!from.eof();from.get(next)) line.push_back(next);
	return line;
}

using namespace boost::spirit;
typedef rule<scanner<string::iterator> > ruleT;

template<typename T> struct setFunctor{
	T** internal;
	setFunctor(T** varPP){internal=varPP;}
	void operator()(T const & newval) const {**internal=newval;}
};
template<typename containerT, typename elementT> struct appendFunctor{
	containerT** internal;
	appendFunctor(containerT** containerPP){internal=containerPP;}
	void operator()(elementT newval) const {(**internal).push_back(newval);}
};
template<typename callbackT, typename elemT> struct creatorCaller{
	callbackT* tobecalled;
	creatorCaller(callbackT* requestor){tobecalled=requestor;}
	void operator()(elemT const & newval) const {tobecalled->callback();}
};

struct bugItem {
	double price;
	unsigned ArtNum;
	string desc;
	virtual bool operator<(bugItem const & other) {return price < other.price;}
	virtual bool operator==(bugItem const & other) {return ArtNum==other.ArtNum;}
};
struct hddItem :public bugItem{
	double GB;
	virtual bool operator<(hddItem const & oHdd) {
		return price/GB < oHdd.price/oHdd.GB;
	}
};
struct ramItem :public bugItem {
	double MB;
	virtual bool operator<(ramItem const & other) {
		return price/MB < other.price/other.MB;
	}
};


class bugItemCreator {
protected:
	bugItem** newbi;
	double *price;
	unsigned *ArtNum;
	string *desc;
public:
	bugItemCreator(bugItem **newbiExt) {newbi=newbiExt;}
	virtual void callback() {
		if(*newbi!=0) delete *newbi;
		*newbi=new bugItem;
		price=&((*newbi)->price);
		ArtNum=&((*newbi)->ArtNum);
		desc=&((*newbi)->desc);
	}
	virtual ruleT getRule() {
		return real_p[creatorCaller<bugItemCreator,double>(this)][setFunctor<double>(&price)] >> *space_p >> str_p("EUR") >>*space_p>> uint_p[setFunctor<unsigned>(&ArtNum)] >> *space_p>>
			*print_p[appendFunctor<string,char>(&desc)];
	}
};

class hddItemCreator:public bugItemCreator {
	double *GB;
public:
	hddItemCreator(bugItem **newbiExt) :bugItemCreator(newbiExt) {}
	virtual void callback() {
		if(*newbi!=0) delete *newbi;
		*newbi=new hddItem;
		price=&((*newbi)->price);
		ArtNum=&((*newbi)->ArtNum);
		desc=&((*newbi)->desc);
		GB=&(dynamic_cast<hddItem>(**newbi).GB);
	}
	virtual ruleT getRule() {
		return real_p[creatorCaller<bugItemCreator,double>(this)][setFunctor<double>(&price)] >> *space_p >> str_p("EUR") >>*space_p>> uint_p[setFunctor<unsigned>(&ArtNum)] >> *space_p>>real_p[setFunctor<double>(&GB)]>>str_p("GB IDE ")>>
			*print_p[appendFunctor<string,char>(&desc)];
	}
};


int main(int argc, char ** argv) {
    vector<bugItem> items;
	bugItem *newItem;
	bugItemCreator bic(&newItem);
	hddItemCreator hic(&newItem);
	ruleT theRule=(hic.getRule() | bic.getRule());
	while(!cin.eof()) {
		string line=myGetline(cin);
		if(parse(line.begin(),line.end(),theRule).full) cout<< typeid(*newItem).name() << ": "<<line;
	}
/*
	cout << "Items:" << endl;
	sort(items.begin(),items.end());
	for(vector<simpleRecord>::iterator it=items.begin();it<items.end();it++) {
		cout << (*it).price << " ";
		if((*it).rtype == HDD) cout<< (*it).subs.hdd.GB;
		else if((*it).rtype == RAM) cout << (*it).subs.ram.MB;
		cout << " ("<< (*it).ArtNum <<") " << (*it).desc<<endl;
	}


	return 0;*/
}