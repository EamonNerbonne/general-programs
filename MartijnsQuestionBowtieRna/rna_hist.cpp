/* 
Counts the number of bases per position in a bowtie-generated RNA aligmnent file, outputting these stats in CSV format.
Expects the first line to be a starting index, then a strand and finally the mapped read.
Also expects EITHER a genome-size in bases OR a .fna genome file.
After these options, any other arguments are taken as input bowtie alignment files.
Bowtie settings to use; '--suppress=1,3,6,7,8'

(C) 2012 Martijn Herber
*/

#define _SCL_SECURE_NO_WARNINGS
#include <iostream>
#include<fstream>
#include <string>
#include <vector>
#include <boost/lexical_cast.hpp>
#include <boost/algorithm/string.hpp>
#include <boost/program_options.hpp>
#include <boost/foreach.hpp>
using std::cout;
using std::cerr;
using std::cin;
using std::ifstream;
using std::ofstream;
using std::istream;
using std::ostream;
using std::string;
using std::vector;
using boost::lexical_cast;
using std::getline;
using boost::split;
using boost::is_any_of;

namespace po = boost::program_options;

#define MAXGENOMELENGTH 1000000000
#define MAXREADLENGTH 2000

#define ARRAYLENGTH 6
#define A_INDEX 0
#define T_INDEX 1
#define G_INDEX 2
#define C_INDEX 3
#define N_INDEX 4
#define TOT_INDEX 5

struct BaseCounts {
	int A,T, G,C,N;

	int Tot() const { return A+T+G+C;}

	BaseCounts() :A(0),T(0),G(0),C(0),N(0) {}
};

void count_bases(string const & read, vector<BaseCounts> & base_counts, size_t offset) {
	for(size_t strI=0; strI < read.size(); strI++) {
		if (read[strI] == 'A') base_counts[offset+strI].A++;
		else if (read[strI] == 'T') base_counts[offset+strI].T++;
		else if (read[strI] == 'G') base_counts[offset+strI].G++;
		else if (read[strI] == 'C') base_counts[offset+strI].C++;
		else if (read[strI] == 'N') base_counts[offset+strI].N++;
		else cerr<<"Invalid base "<<read[strI]<<" in position "<<offset+strI<<", skipping.\n";
	}
}

void output_csv(vector<BaseCounts> const & f_reads, vector<BaseCounts> const & r_reads, ostream & output) {
	output << "pos,A,T,G,C,N,Tot\n";
	for(size_t i=0;i<f_reads.size();i++) {
		auto f = f_reads[i];
		auto r = r_reads[i];
		output 
			<< i << ","

			<< f.A << ","
			<< f.T << ","
			<< f.G << ","
			<< f.C << ","
			<< f.N << ","
			<< f.Tot() << ","

			<< r.A << ","
			<< r.T << ","
			<< r.G << ","
			<< r.C << ","
			<< r.N << ","
			<< r.Tot()	<<"\n";
	}
}

int process_bowtie(long genomelength, istream & input, ostream & output ){
	int n = 0;
	int strandP=0;
	int strandN=0;

	auto Freads = vector<BaseCounts>(genomelength);
	auto Rreads = vector<BaseCounts>(genomelength);


	string line;
	while (getline(input, line)) {
		if(line.size() ==0) continue;
		vector<string> fields;
		n++;
		split( fields, line, is_any_of("\t") );

		if(fields.size() !=3) {
			cerr << "Strand format error on line "<<n<<"!\n";
			return 0;
		} else {
			int pos = lexical_cast<int>(fields[1]);

			if (fields[0] == "+") {
				strandP++;
				count_bases(fields[2],Freads, pos);
			} else if ( fields[0] == "-" ) {
				strandN++;
				count_bases(fields[2],Rreads, pos);
			} else {
				cerr << "Strand format error on line "<<n<<"!\n";
				cerr << "Token was; '"<<fields[0]<<"'\n";
				return 0;
			}
		}
	}


	cerr<<"There were "<<strandP<<" forward reads and "<<strandN <<" backward!\n"		;
	output_csv(Freads,Rreads,output);
	return n;
}

void print_usage(void) {
	cerr << "Usage: rnaseq_hist GENOMELENGTH BOWTIEFILE1 [ BOWTIEFILE2 ... BOWTIEFILEN ]\n";
	cerr << "Will write CSV output to a new file with the same basename, ending in *.hist.csv\n";
	cerr << "Or if only a genomelength is specified will read from stdin and write to stdout.\n";  
}

int main(int argc, char *argv[]){
	int genomelength = 0;
	bool check_errors = false;
	string end;

	po::options_description dsc("Allowed options");
	dsc.add_options()
		("help", "produce this help message")
		("size", po::value<int>(&genomelength), "genome size to map to")
		("fna",  po::value< vector<string> >(), "fna file to infer genome size and check errors")
		("errors", po::value<bool>(&check_errors), "check for errors")
		("bowtiefiles", po::value<vector<string>>(), "bowtiefiles to map to histogram")
		;
	po::positional_options_description p;
	p.add("bowtiefile", -1);

	po::variables_map vm;
	po::store(po::command_line_parser(argc, argv).options(dsc).positional(p).run(), vm);
	po::notify(vm);
	if (vm.count("help")) {
		cerr << dsc << "\n";
		exit(1);
	} else if ( !vm.count("size") && !vm.count("fna") ){
		cerr << "Need to specify genome size or fna genome reference file!\n" << "\n";
		cerr << dsc << "\n";
		exit(1);
	} else if ( vm.size() == 0 ) {
		cerr << "No arguments! Need to specify at least genome size/ref file and input bowtie file!" << "\n";
		cerr << dsc << "\n";
		exit(1);
	} else if ( genomelength <= 0 || genomelength > MAXGENOMELENGTH ) {
		cerr<<"genome length (" <<genomelength<<") out of bounds (0, "<<MAXGENOMELENGTH<<"]\n";
		exit(1);
	}

	if (vm.size() == 2){ // no input files were given, assume we can read from stdin
		int reads = process_bowtie(genomelength,cin,cout);
		if (reads == 0) {
			cerr << "There was a problem processing bowtiefile read from standard input\n"; 
			exit(1);
		}
		exit(0);
	} else {
		for(string bowtiefile : vm["bowtiefiles"].as<vector<string>>()) {
			cout<< "Processing bowtiefile: "<< bowtiefile <<"\n";
			ifstream bowtiefp(bowtiefile);
			if(!bowtiefp.is_open()) {
				cerr<< "Unable to open bowtiefile: "<< bowtiefile <<"\n";
				exit(1);
			}
			string csvname = bowtiefile + string(".hist.csv");
			ofstream csvfp(csvname);
			if(!csvfp.is_open()) {
				cerr<< "Unable to open csvfile for bowtiefile: "<< bowtiefile <<"\n";
				exit(1);
			}

			int reads = process_bowtie(genomelength,bowtiefp,csvfp);
			if (reads == 0) {
				cerr << "There was a problem processing bowtiefile read from standard input\n"; 
				exit(1);
			}
			cout<< "Processed " << reads <<" reads!";
		}
		exit(0);
	} 
}

