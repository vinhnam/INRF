using System;
using System.Collections.Generic;

namespace BongTagger
{
    public class Word
    {
        public Word()
        {
            AssociatedTagDict = new Dictionary<Tag, int>();
            WordCount = 0;
        }

        public string Value { get; set; }
        public Int64 WordCount { get; private set; }
        public Dictionary<Tag, int> AssociatedTagDict { get; private set; }

        public void AddTag(Tag tag)
        {
            if (AssociatedTagDict.ContainsKey(tag))
            {
                //add 1 count
                AssociatedTagDict[tag]++;
            }
            else
            {
                //and new tag into dictionary
                AssociatedTagDict.Add(tag, 1);
            }
            WordCount++;
        }

        /// <summary>
        /// Caculate P(word|tag)
        /// </summary>
        /// <param name="tag"></param>
        public double GetProbabilityTagGivenWord(Tag tag)
        {
            if (AssociatedTagDict.ContainsKey(tag))
            {
                return (double)AssociatedTagDict[tag] / WordCount;
            }
            return 0;
        }
    }
}