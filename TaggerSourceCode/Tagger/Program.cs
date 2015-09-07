using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tagger
{
    internal class Program
    {
        private readonly Stopwatch _sw = new Stopwatch();
        private bool _hasInputTrainingSet = false;

        private static void Main(string[] args)
        {
            var program = new Program();
            //open main  menu
            //program.OpenMenu();

            program.Test();
        }

        private void Test()
        {
            //tagger
            var tagger = new BongTagger.Tagger();
            //load training set
            var directory = @"F:\MSE\TextMining\brown";
            RecordTime(() =>
            {
                //training
                tagger.Train(Directory.GetFiles(directory).Select(File.ReadAllText).ToList());
            });
            Console.WriteLine("Token Count:{0}", tagger.WordCount);
            Console.WriteLine("Word Count:{0}", tagger.DistinctWordCount);
            Console.WriteLine("Tag Count:{0}", tagger.DistinctTagCount);
            Console.WriteLine("Word Total Count:{0}", tagger.DistinctTagCount);
            Console.WriteLine("Tag Total Count:{0}", tagger.DistinctTagCount);

            var word = "dog";
            var tag = "nn";
            var pWordGTag = tagger.GetProbabilityWordGivenTag(word, tag);
            var pTagGWord = tagger.GetProbabilityTagGivenWord(word, tag);
            Console.WriteLine("p(word=\"{0}\"|tag=\"{1}\"):{2}", word, tag, pWordGTag);
            Console.WriteLine("p(tag=\"{0}\"|word=\"{1}\"):{2}", tag, word, pTagGWord);

            var testSen = tagger.TrainingSentenceList.Where(m => m.Contains("many-much")).ToList();
            var pro2 = tagger.GetProbabilitySequence2Tag("cc", "nn");
            var pro3 = tagger.GetProbabilitySequence3Tag("cc", "nn", "in");

            var exp1 = Math.Log(0);
            var exp2 = Math.Log(20);
            var exp3 = Math.Log(20*10);
            var ex = exp3 - exp2 - exp1;

            bool isSuccess;
            string message;
            tagger.TagDict.OrderBy(m => m.Key).Select(m => m.Key).ToList().ForEach(m => Debug.Print(m));
            var result = tagger.GetTaggedSentence(tagger.TrainingSentenceList[4], out isSuccess, out message);

            var test = tagger.GetTaggedSentence("He is a good person", out isSuccess, out message);

            var test2 = tagger.GetTaggedSentence("Do I know you?", out isSuccess, out message);

            var test3 = tagger.GetTaggedSentence("He is really good but I think he is still not good enough",
                out isSuccess, out message);

            var isGood = tagger.TestSentenceBySentenceLevel(tagger.TrainingSentenceList[4],
                tagger.TrainingTaggedSentenceList[4]);

            var correctSCount = 0;
            for (var i = 0; i < tagger.TrainingSentenceList.Count; i++)
            {
                if (tagger.TestSentenceBySentenceLevel(tagger.TrainingSentenceList[i],
                    tagger.TrainingTaggedSentenceList[i]))
                {
                    correctSCount++;
                }
                Debug.Print("Correct percentage: {0}/{1} = {2}%", correctSCount, (i + 1),
                    (double) correctSCount*100/(i + 1));
            }
            Int64 correctCount = 0;
            Int64 totalCount = 0;
            Int64 totalCorrectCount = 0;
            for (var i = 0; i < tagger.TrainingSentenceList.Count; i++)
            {
                Int64 wrongCount;
                tagger.TestSentenceByWordLevel(tagger.TrainingSentenceList[i],
                    tagger.TrainingTaggedSentenceList[i], out correctCount, out wrongCount);
                totalCount += correctCount + wrongCount;
                totalCorrectCount += correctCount;
                Debug.Print("Correct percentage: {0}/{1} = {2}%", totalCorrectCount, totalCount,
                    (double) totalCorrectCount*100/totalCount);
            }

            Debug.Print(test);
            Debug.Print(test2);
            Debug.Print(test3);
            Debug.Print("Correct percentage: {0}/{1} = {2}%", correctCount, tagger.TrainingSentenceList.Count,
                (double) correctCount/tagger.TrainingSentenceList.Count);
            Console.ReadLine();
        }

        private void OpenMenu()
        {
            var contents = new List<string>
            {
                "Main functions:",
                "1. Input training set folder",
                "2. Tag input sentence",
                "3. Get probability of word in tag",
                "4. Get word list by tag",
                "5. Get tag sequence list",
                "6. Clear screen",
                "7. Exit program"
            };

            contents.ForEach(Console.WriteLine);

            //read input command by users
            ReadInputCommand();
        }

        private void ReadInputCommand()
        {
            while (true)
            {
                var validCommand = new[] {'1', '2', '3', '4', '5', '6', '7'};
                Console.WriteLine("Please input choose function (1 to 7) and press Enter key:");
                var inputCode = Console.ReadKey().KeyChar;
                if (validCommand.Contains(inputCode))
                {
                    Console.WriteLine();
                    switch (inputCode)
                    {
                        case '1':
                            //GetTrainingSet();
                            break;
                        case '2':
                            //GetTag();
                            break;
                        case '3':
                            //GetTagWordProbability();
                            break;
                        case '4':
                            //GetWordListByTag();
                            break;
                        case '5':
                            //GetTagSequenceList();
                            break;
                        case '6':
                            Console.Clear();
                            break;
                        case '7':
                            return;
                    }

                    OpenMenu();
                }
                else
                {
                    Console.WriteLine();
                    continue;
                }
                break;
            }
        }

        public void RecordTime(Action action)
        {
            _sw.Restart();
            action();
            Console.WriteLine("Elapsed Time: {0}s", (double) _sw.ElapsedMilliseconds/1000);
            _sw.Stop();
        }
    }
}