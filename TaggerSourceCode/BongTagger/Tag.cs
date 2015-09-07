using System;
using System.Collections.Generic;

namespace BongTagger
{
    public class Tag
    {
        public Tag()
        {
            AssociatedWordDict = new Dictionary<Word, int>();
            TagCount = 0;
        }

        public string Value { get; set; }
        public Int64 TagCount { get; private set; }
        public Dictionary<Word, int> AssociatedWordDict { get; private set; }

        public void AddWord(Word word)
        {
            if (AssociatedWordDict.ContainsKey(word))
            {
                //add 1 count
                AssociatedWordDict[word]++;
            }
            else
            {
                //and new tag into dictionary
                AssociatedWordDict.Add(word, 1);
            }
            TagCount++;
        }

        /// <summary>
        /// Caculate P(word|tag)
        /// </summary>
        /// <param name="word"></param>
        public double GetProbabilityWordGivenTag(Word word)
        {
            if (AssociatedWordDict.ContainsKey(word))
            {
                return (double) AssociatedWordDict[word]/TagCount;
            }
            return 0;
        }
    }
}