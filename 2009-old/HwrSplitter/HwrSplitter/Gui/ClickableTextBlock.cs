using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;

namespace HwrSplitter.Gui
{
    public class ClickableTextBlock
    {
        Run selectedRun;
        Brush selectedBackground = Brushes.Yellow;
        TextBlock textBlock;
        Run[] runs;

        public Brush SelectedBackground { get { return selectedBackground; } set { selectedBackground = value; if (selectedRun != null) selectedRun.Background = selectedBackground; } }
        public bool IgnoreClicksOnSelectedWord { get; set; }
        public event Action<int, string> WordClicked;
        public int WordCount { get { return runs.Length; } }
        public IEnumerable<string> Words { get { return runs.Select(r => r.Text); } }

        public ClickableTextBlock(TextBlock textBlock) {
            this.textBlock = textBlock;
            IgnoreClicksOnSelectedWord = true;
            textBlock.MouseLeftButtonDown += onMouseDown;
        }

        public void WireUpTextBlock(string[] words) {
            var inlines = textBlock.Inlines;
            inlines.Clear();
            runs = new Run[words.Length];

            for (int i = 0; i < words.Length; i++) {
                var wordRun = new Run(words[i]) { Tag = i };
                runs[i] = wordRun;
                inlines.Add(wordRun);

                if (i < words.Length - 1)
                    inlines.Add(new Run(" "));
            }
        }

        public int? SelectedIndex {
            get {
                return selectedRun == null ? null : (int?)selectedRun.Tag;
            }
            set {
                var newTarget = value.HasValue ? runs[value.Value] : null;//we execute this first to throw early in case of IndexOutOfBounds
                if (selectedRun != null)
                    selectedRun.Background = Brushes.Transparent;
                selectedRun = newTarget;
                if (selectedRun != null)
                    selectedRun.Background = SelectedBackground;
            }
        }

        void onMouseDown(object sender, MouseButtonEventArgs e) {
            Run newTarget = e.OriginalSource as Run;
            if (WordClicked != null &&
                newTarget != null &&
                newTarget.Text != " " && //TODO: not necessary due to next test?
                newTarget.Tag is int &&
                !(newTarget == selectedRun && IgnoreClicksOnSelectedWord)
                ) {
                WordClicked((int)newTarget.Tag, newTarget.Text);
            }
        }
    }
}
