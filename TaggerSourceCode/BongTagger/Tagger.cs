using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace BongTagger
{
    public class Tagger
    {
        public Tagger()
        {
            WordDict = new Dictionary<string, Word>();
            TagDict = new Dictionary<string, Tag>();
            TrainingSentenceList = new List<string>();
            TrainingTaggedSentenceList = new List<string>();
            Seq2TagDict = new Dictionary<string, Sequence2Tag>();
            Seq3TagDict = new Dictionary<string, Sequence3Tag>();
            TagDescDict = new Dictionary<string, string>();

            //set default value for lamda
            Lamda1 = (double)1 / 3;
            Lamda2 = (double)1 / 3;
            Lamda3 = (double)1 / 3;

            //add blank tag
            DumpTag = new Tag() { Value = "Dump" };

            //get tag dict desc
            MakeTagDescDictionary();
        }

        #region Propeties

        public Dictionary<string, string> TagDescDict { get; set; } 

        /// <summary>
        /// dictionary of word in training set
        /// </summary>
        public Dictionary<string, Word> WordDict { get; set; }

        /// <summary>
        /// dictionary of tag in traning set
        /// </summary>
        public Dictionary<string, Tag> TagDict { get; set; }

        /// <summary>
        /// dictionary of sequence 2 tag in traning set
        /// </summary>
        private Dictionary<string, Sequence2Tag> Seq2TagDict { get; set; }

        /// <summary>
        /// dictionary of sequence 3 tag in traning set
        /// </summary>
        private Dictionary<string, Sequence3Tag> Seq3TagDict { get; set; }

        /// <summary>
        /// list of all training sentences
        /// </summary>
        public List<string> TrainingSentenceList { get; set; }

        /// <summary>
        /// list of all training tagged sentences
        /// </summary>
        public List<string> TrainingTaggedSentenceList { get; set; }

        public Int64 WordCount { get; set; }

        public Int64 DistinctWordCount
        {
            get { return WordDict.Count; }
        }
        public Int64 DistinctTagCount
        {
            get { return TagDict.Count; }
        }
        public List<string> WordList
        {
            get { return WordDict.OrderBy(m => m.Key).Select(m => m.Key).ToList(); }
        }
        public List<string> TagList
        {
            get { return TagDict.OrderBy(m => m.Key).Select(m => m.Key).ToList(); }
        }
        public List<string> Sequence2TagList
        {
            get { return Seq2TagDict.OrderBy(m => m.Key).Select(m => m.Key).ToList(); }
        }
        public List<string> Sequence3TagList
        {
            get { return Seq3TagDict.OrderBy(m => m.Key).Select(m => m.Key).ToList(); }
        }

        private double Lamda1 { get; set; }
        private double Lamda2 { get; set; }
        private double Lamda3 { get; set; }

        private Tag DumpTag { get; set; }

        const string SeparteStr = "/";

        #endregion

        #region Training set

        /// <summary>
        /// train program with list of source string
        /// </summary>
        /// <param name="sourceList"></param>
        public void Train(List<string> sourceList)
        {
            foreach (var source in sourceList)
            {
                Train(source);
            }
        }

        /// <summary>
        ///     train program with source string
        /// </summary>
        /// <param name="source"></param>
        public void Train(string source)
        {
            //break source in to sentence
            string[] breakLine = { "\n" };
            char[] spaceChar = {'\t', ' ' };
            const char separateChar = '/';
            foreach (var sentence in source.Split(breakLine, StringSplitOptions.RemoveEmptyEntries))
            {
                if (String.IsNullOrEmpty(sentence.Trim())) continue;
                var fullSentence = sentence + " " + breakLine[0];
                var originSentence = "";
                var taggedOriginSentence = "";
                var tagList = new List<Tag>();
                //break sentence into list of word/tag
                foreach (var wordTag in fullSentence.Split(spaceChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    var lastIndex = wordTag.LastIndexOf(separateChar);
                    if (lastIndex < 0) continue;
                    //add to word and tag dictionary
                    var wordStr = wordTag.Substring(0, lastIndex);
                    var tagStr = GetFormattedTag(wordTag.Substring(lastIndex + 1, wordTag.Length - lastIndex - 1));
                    var word = AddWord(wordStr);
                    var tag = AddTag(tagStr);
                    word.AddTag(tag);
                    tag.AddWord(word);
                    originSentence += " " + wordStr;
                    taggedOriginSentence += " " + wordStr + separateChar + tagStr;
                    tagList.Add(tag);
                    WordCount++;
                }

                //add sequence 2 tag
                AddSequence2TagFromTagList(tagList);

                //add sequence 3 tag
                AddSequence3TagFromTagList(tagList);

                //add origin senetence
                TrainingSentenceList.Add(originSentence.Trim());

                //add origin senetence
                TrainingTaggedSentenceList.Add(taggedOriginSentence.Trim());
            }
        }

        private string GetFormattedTag(string p)
        {
            return p.ToLower();
            //.Replace("fw-", "")
            //.Split('-')[0];
        }

        /// <summary>
        /// add to sequence 2 tag by sentence (tag list)
        /// </summary>
        /// <param name="tagList"></param>
        private void AddSequence2TagFromTagList(List<Tag> tagList)
        {
            if (tagList.Count < 1) return;
            AddSequence2Tag(DumpTag, DumpTag);
            AddSequence2Tag(DumpTag, tagList[0]);

            for (var i = 0; i < tagList.Count - 1; i++)
            {
                AddSequence2Tag(tagList[i], tagList[i + 1]);
            }
        }

        /// <summary>
        /// add to sequence 2 tag to dictionary
        /// </summary>
        /// <param name="tag1"></param>
        /// <param name="tag2"></param>
        private void AddSequence2Tag(Tag tag1, Tag tag2)
        {
            var key = tag1.Value + SeparteStr + tag2.Value;
            if (Seq2TagDict.ContainsKey(key))
            {
                Seq2TagDict[key].Count++;
            }
            else
            {
                var seq = new Sequence2Tag()
                {
                    Tag1 = tag1,
                    Tag2 = tag2,
                    Count = 1
                };
                Seq2TagDict.Add(key, seq);
            }
        }

        /// <summary>
        /// add to sequence 3 tag by sentence (tag list)
        /// </summary>
        /// <param name="tagList"></param>
        private void AddSequence3TagFromTagList(List<Tag> tagList)
        {
            if (tagList.Count < 1) return;
            AddSequence3Tag(DumpTag, DumpTag, tagList[0]);

            if (tagList.Count < 2) return;
            AddSequence3Tag(DumpTag, tagList[0], tagList[1]);

            for (var i = 0; i < tagList.Count - 2; i++)
            {
                AddSequence3Tag(tagList[i], tagList[i + 1], tagList[i + 2]);
            }
        }

        /// <summary>
        /// add to sequence 3 tag to dictionary
        /// </summary>
        /// <param name="tag1"></param>
        /// <param name="tag2"></param>
        /// <param name="tag3"></param>
        private void AddSequence3Tag(Tag tag1, Tag tag2, Tag tag3)
        {
            var key = tag1.Value + SeparteStr + tag2.Value + SeparteStr + tag3.Value;
            if (Seq3TagDict.ContainsKey(key))
            {
                Seq3TagDict[key].Count++;
            }
            else
            {
                var seq = new Sequence3Tag()
                {
                    Tag1 = tag1,
                    Tag2 = tag2,
                    Tag3 = tag3,
                    Count = 1
                };
                Seq3TagDict.Add(key, seq);
            }
        }
        /// <summary>
        /// clean trai ning set
        /// </summary>
        public void CleanTrainingSet()
        {
            WordDict.Clear();
            TagDict.Clear();
            Seq2TagDict.Clear();
            Seq3TagDict.Clear();
            TrainingSentenceList.Clear();
            WordCount = 0;
        }

        private void MakeTagDescDictionary()
        {
            TagDescDict.Add(".", "sentence closer (. ; ? *)");
            TagDescDict.Add("(", "left paren");
            TagDescDict.Add(")", "right paren");
            TagDescDict.Add("*", "not, n't");
            TagDescDict.Add("--", "dash");
            TagDescDict.Add(",", "comma");
            TagDescDict.Add(":", "colon");
            TagDescDict.Add("ABL", "pre-qualifier (quite, rather)");
            TagDescDict.Add("ABN", "pre-quantifier (half, all)");
            TagDescDict.Add("ABX", "pre-quantifier (both)");
            TagDescDict.Add("AP", "post-determiner (many, several, next)");
            TagDescDict.Add("AT", "article (a, the, no)");
            TagDescDict.Add("BE", "be");
            TagDescDict.Add("BED", "were");
            TagDescDict.Add("BEDZ", "was");
            TagDescDict.Add("BEG", "being");
            TagDescDict.Add("BEM", "am");
            TagDescDict.Add("BEN", "been");
            TagDescDict.Add("BER", "are, art");
            TagDescDict.Add("BEZ", "is");
            TagDescDict.Add("CC", "coordinating conjunction (and, or)");
            TagDescDict.Add("CD", "cardinal numeral (one, two, 2, etc.)");
            TagDescDict.Add("CS", "subordinating conjunction (if, although)");
            TagDescDict.Add("DO", "do");
            TagDescDict.Add("DOD", "did");
            TagDescDict.Add("DOZ", "does");
            TagDescDict.Add("DT", "singular determiner/quantifier (this, that)");
            TagDescDict.Add("DTI", "singular or plural determiner/quantifier (some, any)");
            TagDescDict.Add("DTS", "plural determiner (these, those)");
            TagDescDict.Add("DTX", "determiner/double conjunction (either)");
            TagDescDict.Add("EX", "existential there");
            TagDescDict.Add("FW", "foreign word (hyphenated before regular tag)");
            TagDescDict.Add("HV", "have");
            TagDescDict.Add("HVD", "had (past tense)");
            TagDescDict.Add("HVG", "having");
            TagDescDict.Add("HVN", "had (past participle)");
            TagDescDict.Add("IN", "preposition");
            TagDescDict.Add("JJ", "adjective");
            TagDescDict.Add("JJR", "comparative adjective");
            TagDescDict.Add("JJS", "semantically superlative adjective (chief, top)");
            TagDescDict.Add("JJT", "morphologically superlative adjective (biggest)");
            TagDescDict.Add("MD", "modal auxiliary (can, should, will)");
            TagDescDict.Add("NC", "cited word (hyphenated after regular tag)");
            TagDescDict.Add("NN", "singular or mass noun");
            TagDescDict.Add("NN$", "possessive singular noun");
            TagDescDict.Add("NNS", "plural noun");
            TagDescDict.Add("NNS$", "possessive plural noun");
            TagDescDict.Add("NP", "proper noun or part of name phrase");
            TagDescDict.Add("NP$", "possessive proper noun");
            TagDescDict.Add("NPS", "plural proper noun");
            TagDescDict.Add("NPS$", "possessive plural proper noun");
            TagDescDict.Add("NR", "adverbial noun (home, today, west)");
            TagDescDict.Add("OD", "ordinal numeral (first, 2nd)");
            TagDescDict.Add("PN", "nominal pronoun (everybody, nothing)");
            TagDescDict.Add("PN$", "possessive nominal pronoun");
            TagDescDict.Add("PP$", "possessive personal pronoun (my, our)");
            TagDescDict.Add("PP$$", "second (nominal) possessive pronoun (mine, ours)");
            TagDescDict.Add("PPL", "singular reflexive/intensive personal pronoun (myself)");
            TagDescDict.Add("PPLS", "plural reflexive/intensive personal pronoun (ourselves)");
            TagDescDict.Add("PPO", "objective personal pronoun (me, him, it, them)");
            TagDescDict.Add("PPS", "3rd. singular nominative pronoun (he, she, it, one)");
            TagDescDict.Add("PPSS", "other nominative personal pronoun (I, we, they, you)");
            TagDescDict.Add("PRP", "Personal pronoun");
            TagDescDict.Add("PRP$", "Possessive pronoun");
            TagDescDict.Add("QL", "qualifier (very, fairly)");
            TagDescDict.Add("QLP", "post-qualifier (enough, indeed)");
            TagDescDict.Add("RB", "adverb");
            TagDescDict.Add("RBR", "comparative adverb");
            TagDescDict.Add("RBT", "superlative adverb");
            TagDescDict.Add("RN", "nominal adverb (here, then, indoors)");
            TagDescDict.Add("RP", "adverb/particle (about, off, up)");
            TagDescDict.Add("TO", "infinitive marker to");
            TagDescDict.Add("UH", "interjection, exclamation");
            TagDescDict.Add("VB", "verb, base form");
            TagDescDict.Add("VBD", "verb, past tense");
            TagDescDict.Add("VBG", "verb, present participle/gerund");
            TagDescDict.Add("VBN", "verb, past participle");
            TagDescDict.Add("VBP", "verb, non 3rd person, singular, present");
            TagDescDict.Add("VBZ", "verb, 3rd. singular present");
            TagDescDict.Add("WDT", "wh- determiner (what, which)");
            TagDescDict.Add("WP$", "possessive wh- pronoun (whose)");
            TagDescDict.Add("WPO", "objective wh- pronoun (whom, which, that)");
            TagDescDict.Add("WPS", "nominative wh- pronoun (who, which, that)");
            TagDescDict.Add("WQL", "wh- qualifier (how)");
            TagDescDict.Add("WRB", "wh- adverb (how, where, when)");
        }
        #endregion

        #region Add, Find word, tag

        public Word AddWord(string word)
        {
            if (WordDict.ContainsKey(word))
            {
                return WordDict[word];
            }
            var newWord = new Word { Value = word };
            WordDict.Add(word, newWord);
            return newWord;
        }

        public Tag AddTag(string tag)
        {
            if (TagDict.ContainsKey(tag))
            {
                return TagDict[tag];
            }
            var newTag = new Tag { Value = tag };
            TagDict.Add(tag, newTag);
            return newTag;
        }

        /// <summary>
        /// find word
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public Word FindWord(string word)
        {
            return WordDict.ContainsKey(word) ? WordDict[word] : null;
        }

        /// <summary>
        /// find tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public Tag FindTag(string tag)
        {
            return TagDict.ContainsKey(tag) ? TagDict[tag] : null;
        }
        #endregion

        #region Caculate probability

        /// <summary>
        /// set lamda value
        /// </summary>
        /// <param name="lamda1">for tri-gram</param>
        /// <param name="lamda2">for bi-gram</param>
        /// <param name="lamda3">for uni-gram</param>
        public void SetLamda(double lamda1, double lamda2, double lamda3)
        {
            var total = lamda1 + lamda2 + lamda3;
            Lamda1 = lamda1 / total;
            Lamda2 = lamda2 / total;
            Lamda3 = lamda3 / total;
        }

        /// <summary>
        ///     Caculate P(word|tag)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="tag"></param>
        public double GetProbabilityWordGivenTag(string word, string tag)
        {
            if (!WordDict.ContainsKey(word) || !TagDict.ContainsKey(tag))
            {
                return 0; //return 0 if could not find word or tag
            }
            return GetProbabilityWordGivenTag(WordDict[word], TagDict[tag]);
        }

        /// <summary>
        /// Caculate P(word|tag)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="tag"></param>
        private double GetProbabilityWordGivenTag(Word word, Tag tag)
        {
            return (tag.GetProbabilityWordGivenTag(word));
        }

        /// <summary>
        /// Caculate P(word|tag)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="tag"></param>
        public double GetProbabilityTagGivenWord(string word, string tag)
        {
            if (!WordDict.ContainsKey(word) || !TagDict.ContainsKey(tag))
            {
                return 0; //return 0 if could not find word or tag
            }
            return GetProbabilityTagGivenWord(WordDict[word], TagDict[tag]);
        }

        /// <summary>
        /// Caculate P(word|tag)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="tag"></param>
        private double GetProbabilityTagGivenWord(Word word, Tag tag)
        {
            return (word.GetProbabilityTagGivenWord(tag));
        }

        /// <summary>
        /// Caculate P(tag2|tag1)
        /// Equation: P(tag2|tag1) = lamda2 * (Count(tag1,tag2)/Count(tag1)) + lamda3 * (Count(tag2)/Count(total tags))
        /// </summary>
        /// <param name="tag1Str"></param>
        /// <param name="tag2Str"></param>
        /// <returns></returns>
        public double GetProbabilitySequence2Tag(string tag1Str, string tag2Str)
        {
            var tag1 = TagDict.ContainsKey(tag1Str) ? TagDict[tag1Str] : null;
            var tag2 = TagDict.ContainsKey(tag2Str) ? TagDict[tag2Str] : null;
            return GetProbabilitySequence2Tag(tag1, tag2);
        }

        /// <summary>
        /// Caculate P(tag2|tag1)
        /// Equation: P(tag2|tag1) 
        /// = lamda2 * (Count(tag1,tag2)/Count(tag1)) 
        /// + lamda3 * (Count(tag2)/Count(total tags))
        /// </summary>
        /// <param name="tag1"></param>
        /// <param name="tag2"></param>
        /// <returns></returns>
        public double GetProbabilitySequence2Tag(Tag tag1, Tag tag2)
        {
            if (tag2 == null) return 0;

            double firstParam = 0;
            //calculate first param
            if (tag1 != null)
            {
                var key = tag1.Value + SeparteStr + tag2.Value;
                if (Seq2TagDict.ContainsKey(key))
                {
                    firstParam = Lamda2 * Seq2TagDict[key].Count / TagDict[tag1.Value].TagCount;
                }
            }
            //calculate second param
            var secondParam = Lamda3 * tag2.TagCount / WordCount;
            return (firstParam + secondParam) / (Lamda2 + Lamda3);
        }


        /// <summary>
        /// Caculate P(tag3|tag1,tag2)
        /// Equation: P(tag3|tag1,tag2)
        /// = lamda1 * (Count(tag1, tag2,tag3)/Count(tag1,tag2)) 
        /// + lamda2 * (Count(tag2,tag3)/Count(tag2)) 
        /// + lamda3 * (Count(tag3)/Count(total tags))
        /// </summary>
        /// <param name="tag1Str"></param>
        /// <param name="tag2Str"></param>
        /// <param name="tag3Str"></param>
        /// <returns></returns>
        public double GetProbabilitySequence3Tag(string tag1Str, string tag2Str, string tag3Str)
        {
            var tag1 = TagDict.ContainsKey(tag1Str) ? TagDict[tag1Str] : null;
            var tag2 = TagDict.ContainsKey(tag2Str) ? TagDict[tag2Str] : null;
            var tag3 = TagDict.ContainsKey(tag3Str) ? TagDict[tag3Str] : null;
            return GetProbabilitySequence3Tag(tag1, tag2, tag3);
        }

        /// <summary>
        /// Caculate P(tag3|tag1,tag2)
        /// Equation: P(tag3|tag1,tag2)
        /// = lamda1 * (Count(tag1, tag2,tag3)/Count(tag1,tag2)) 
        /// + lamda2 * (Count(tag2,tag3)/Count(tag2)) 
        /// + lamda3 * (Count(tag3)/Count(total tags))
        /// </summary>
        /// <param name="tag1"></param>
        /// <param name="tag2"></param>
        /// <param name="tag3"></param>
        /// <returns></returns>
        public double GetProbabilitySequence3Tag(Tag tag1, Tag tag2, Tag tag3)
        {
            if (tag3 == null) return 0;

            double firstParam = 0;
            double secondParam = 0;

            //calculate first, second param
            if (tag2 != null)
            {
                if (tag1 != null)
                {
                    var key12 = tag1.Value + SeparteStr + tag2.Value;
                    if (Seq2TagDict.ContainsKey(key12))
                    {
                        var key123 = tag1.Value + SeparteStr + tag2.Value + SeparteStr + tag3.Value;
                        if (Seq3TagDict.ContainsKey(key123))
                        {
                            firstParam = Lamda1 * Seq3TagDict[key123].Count / Seq2TagDict[key12].Count;
                        }
                    }
                }

                var key23 = tag2.Value + SeparteStr + tag3.Value;
                if (Seq2TagDict.ContainsKey(key23))
                {
                    if (tag2 == DumpTag)
                    {
                        secondParam = Lamda2*Seq2TagDict[key23].Count / TrainingSentenceList.Count;
                    }
                    else
                    {
                        secondParam = Lamda2 * Seq2TagDict[key23].Count / TagDict[tag2.Value].TagCount;
                    }
                }
            }
            //calculate second param
            var thirdParam = Lamda3 * tag3.TagCount / WordCount;
            return (firstParam + secondParam + thirdParam) / (Lamda1 + Lamda2 + Lamda3);
        }
        #endregion

        #region Get tagged sentences

        /// <summary>
        /// get tagged list
        /// </summary>
        /// <param name="inputWordList"></param>
        /// <param name="isSuccessful"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public List<Tag> GetTagList(List<Word> inputWordList, out bool isSuccessful, out string message)
        {
            //make Viterbi table as array
            var viterbiArr = new List<ViterbiObject>[inputWordList.Count];

            #region initilization
            //add into first position
            viterbiArr[0] = inputWordList[0].AssociatedTagDict.Keys.Select(tag3 => new ViterbiObject()
            {
                Tag1 = DumpTag, 
                Tag2 = DumpTag, 
                Tag3 = tag3, 
                Pi = Math.Log(GetProbabilitySequence3Tag(DumpTag, DumpTag, tag3))
                    + Math.Log(GetProbabilityWordGivenTag(inputWordList[0], tag3))//use log to store probabilities
            }).ToList();

            //add into second position
            if (inputWordList.Count >= 2)
            {
                viterbiArr[1] = (from tag2 in inputWordList[0].AssociatedTagDict.Keys
                                 from tag3 in inputWordList[1].AssociatedTagDict.Keys
                    let prevViterbi = viterbiArr[0].FirstOrDefault(m => m.Tag3 == tag2)
                    where prevViterbi != null
                    select new ViterbiObject()
                                {
                                    Tag1 = DumpTag,
                                    Tag2 = tag2,
                                    Tag3 = tag3,
                                    Pi = prevViterbi.Pi
                                        + Math.Log(GetProbabilitySequence2Tag(tag2, tag3))
                                        + Math.Log(GetProbabilityWordGivenTag(inputWordList[1], tag3))
                                }).ToList();
            }
            #endregion

            #region calculate Pi and generate viterbi table
            //add from 3 to n
            for (var i = 2; i < inputWordList.Count; i++)
            {
                var viterbiList = new List<ViterbiObject>();

                foreach (var tag3 in inputWordList[i].AssociatedTagDict.Keys)
                {
                    foreach (var tag2 in inputWordList[i - 1].AssociatedTagDict.Keys)
                    {
                        ViterbiObject maxValue = null;
                        foreach (var tag1 in inputWordList[i - 2].AssociatedTagDict.Keys)
                        {
                            var prevViterbi = viterbiArr[i - 1].FirstOrDefault(m => m.Tag3 == tag2 && m.Tag2 == tag1);
                            if (prevViterbi == null) continue;
                            var currentPi = prevViterbi.Pi
                                            + Math.Log(GetProbabilitySequence3Tag(tag1, tag2, tag3))
                                            + Math.Log(GetProbabilityWordGivenTag(inputWordList[i], tag3));
                            if (maxValue == null || maxValue.Pi < currentPi)
                            {
                                maxValue = new ViterbiObject()
                                {
                                    Tag1 = tag1,
                                    Tag2 = tag2,
                                    Tag3 = tag3,
                                    Pi = currentPi
                                };
                            }
                        }
                        viterbiList.Add(maxValue);
                    }
                }
                viterbiArr[i] = viterbiList;
            }

            #endregion

            #region backtrack to find sequence tag

            var tagArr = new Tag[inputWordList.Count];

            //find last tag (n)th
            tagArr[inputWordList.Count - 1] = viterbiArr[inputWordList.Count - 1].MaxBy(m => m.Pi).Tag3;

            //find (n-1)th tag
            if (inputWordList.Count > 1)
            {
                tagArr[inputWordList.Count - 2] = viterbiArr[inputWordList.Count - 1].MaxBy(m => m.Pi).Tag2;
            }

            //find (n-2)th tag
            if (inputWordList.Count > 2)
            {
                tagArr[inputWordList.Count - 3] = viterbiArr[inputWordList.Count - 1].MaxBy(m => m.Pi).Tag1;
            }

            //find remaing tag
            for (var i = inputWordList.Count - 4; i >= 0; i--)
            {
                var viterbiObj =
                    viterbiArr[i + 2].FirstOrDefault(m => m.Tag3 == tagArr[i + 2] && m.Tag2 == tagArr[i + 1]);
                if (viterbiObj != null)
                {
                    tagArr[i] = viterbiObj.Tag1;
                }
            }

            #endregion


            isSuccessful = true;
            message = "";
            return tagArr.ToList();
        }


        /// <summary>
        /// get tagged sentence
        /// </summary>
        /// <param name="inputSentence"></param>
        /// <param name="isSuccessful"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public String GetTaggedSentence(string inputSentence, out bool isSuccessful, out string message)
        {
            List<Tag> tagList;
            return GetTaggedSentence(inputSentence, out tagList, out isSuccessful, out message);
        }

        /// <summary>
        /// get tagged sentence
        /// </summary>
        /// <param name="inputSentence"></param>
        /// <param name="tagList"></param>
        /// <param name="isSuccessful"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public String GetTaggedSentence(string inputSentence, out List<Tag> tagList, out bool isSuccessful, out string message)
        {
            tagList = null;
            //break into word list
            var wordList = new List<Word>();
            foreach (var wordStr in inputSentence.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!WordDict.ContainsKey(wordStr))
                {
                    isSuccessful = false;
                    message = String.Format("Sentence contains new word \"{0}\". Cannot tag it. Please train me with it first.", wordStr);
                    return null;
                }
                wordList.Add(WordDict[wordStr]);
            }

            //return if there is no word
            if (wordList.Count == 0)
            {
                isSuccessful = false;
                message = "The sentence contains no word.";
                return null;
            }
            tagList = GetTagList(wordList, out isSuccessful, out message);

            var taggedSentence = "";
            for (var i = 0; i < wordList.Count; i++)
            {
                taggedSentence += wordList[i].Value + SeparteStr + tagList[i].Value + " ";
            }

            return taggedSentence.Trim();
        }

        public string GetTagDetail(List<Tag> tagList)
        {
            var originTagDict = new HashSet<string>();

            foreach (var tag in tagList)
            {
                var tagValue = tag.Value;
                if (tagValue.Contains("*") && !originTagDict.Contains("*"))
                {
                    originTagDict.Add("*");
                }
                tagValue = tagValue.Replace("*", "")
                    .Replace("fw-", "")
                    .Replace("-hl", "")
                    .Replace("-tl", "")
                    .Replace("-nc", "");
                foreach (var splittedTag in tagValue.Split('+').Where(splittedTag => !originTagDict.Contains(splittedTag)))
                {
                    originTagDict.Add(splittedTag);
                }
            }

            return originTagDict.OrderBy(m => m).Where(tag => TagDescDict.ContainsKey(tag.ToUpper())).Aggregate("", (current, tag) => current + tag.ToUpper()  + " : " + TagDescDict[tag.ToUpper()] + "\n");
        }
        #endregion

        #region Get training set
        /// <summary>
        /// test sentence
        /// </summary>
        /// <param name="inputSentence"></param>
        /// <param name="inputResult"></param>
        public bool TestSentenceBySentenceLevel(string inputSentence, string inputResult)
        {
            bool isSuccessful;
            string message;
            var result = GetTaggedSentence(inputSentence, out isSuccessful, out message);
            return isSuccessful && result.Trim().Equals(inputResult);
        }

        /// <summary>
        /// test sentence
        /// </summary>
        /// <param name="inputSentence"></param>
        /// <param name="inputResult"></param>
        /// <param name="correctCount"></param>
        /// <param name="wrongCount"></param>
        public void TestSentenceByWordLevel(string inputSentence, string inputResult, out Int64 correctCount, out Int64 wrongCount)
        {
            //break into word list
            bool isSuccessful;
            string message;
            var wordList = inputSentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(wordStr => WordDict[wordStr]).ToList();
            var tagList = GetTagList(wordList, out isSuccessful, out message);
            correctCount = 0;
            wrongCount = 0;

            var resultArr = inputResult.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (var i = 0; i < resultArr.Count; i++)
            {
                if (resultArr[i].Equals(wordList[i].Value + SeparteStr + tagList[i].Value))
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                }
            }
        }
        #endregion
    }
}