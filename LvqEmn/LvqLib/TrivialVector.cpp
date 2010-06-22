	#include "stdafx.h"
	class TrivialList
	{
		int count;
	public:
		TrivialList(int count) : count(count){}
		virtual double Average() const=0;
		int Count() const {return count;}
		virtual ~TrivialList(){}
	};


	template<typename TIndexable, typename TBase>
	class AverageHelper : public TBase
	{
	protected:
		template<typename T>
		AverageHelper(T arg) : TBase(arg){}
	public:
		double Average() const{
			TIndexable const & self = static_cast<TIndexable const &>(*this);
			double sum=0.0;
			for(int i=0;i<self.Count();++i) sum += self.Get(i);
			return sum / self.Count();
		}
	};


	class IndexableList :  public AverageHelper<IndexableList, TrivialList>
	{
		std::vector<double> backend;
	public:
		IndexableList(int count) : AverageHelper(count), backend(count) { }
		double & Get(int i) { return backend[i];}
		double const & Get(int i) const { return backend[i];}
};

	IndexableList * MakeList() {return new IndexableList(5);} //error:cannot instantiate abstract class