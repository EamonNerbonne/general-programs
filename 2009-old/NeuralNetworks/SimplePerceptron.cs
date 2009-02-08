using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
namespace NeuralNetworks
{
    public class SimplePerceptron
    {
        public Vector w;
        public SimplePerceptron(int N) { w = new Vector(N); }
        public SimplePerceptron(Vector w) { this.w = w; }
        private int N { get { return w.N; } }

        public double LearningStep(Func<double, double> algorithm, LabelledSample example) {
            var directedSample = example.Sample * example.Label;
            var alignment = w & directedSample;
            w += (1.0 / N * algorithm(alignment)) * directedSample;
            return alignment;
        }

        public double FastRStep(LabelledSample example) {//avoid memory allocation.
            var sample = example.Sample.elems;
            var wA=w.elems;
            double dotprod=0;
            for (int i = 0; i < wA.Length; i++)
                dotprod += sample[i] * example.Label * wA[i];
            if (dotprod <= 0) {
                var scaleFac = example.Label / (double)N;
                for (int i = 0; i < wA.Length; i++)
                    wA[i] += scaleFac * sample[i];
            }
            return dotprod;
        }

        public int DoTraining(DataSet D, int maxEpochs) {//returns 0 if no storage possible; otherwise number of epochs+1
            int unchangedCount=0;// number of consecutively "correct" classifications, updated online.
            for (int n = 0; n < maxEpochs; n++) {
                for (int i = 0; i < D.P; i++) {
                    
                    //if (LearningStep(Rosenblatt0, D.samples[i]) > 0)
                    if (FastRStep(D.samples[i]) > 0)
                        unchangedCount++; //sample is OK and not updated
                    else
                        unchangedCount = 0; //needed learning, reset.

                    if (unchangedCount >= D.P) //all samples work!
                        return n+1;
                }
            }
            return 0;
        }


        public static double Rosenblatt0(double E_mu_t) { return E_mu_t <= 0 ? 1 : 0; }
    }
}
