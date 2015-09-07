using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using BongTagger;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Practices.Prism.Mvvm;

namespace TaggerWPF
{
    internal class TaggerViewModel : BindableBase
    {
        #region properties

        private readonly TaggerService _service;
        private readonly Tagger _tagger;
        private double _elapsedTime;
        private readonly MetroWindow _window;
        private string _trainingFolderPath;

        public string TrainingFolderPath
        {
            get { return _trainingFolderPath; }
            set
            {
                SetProperty(ref _trainingFolderPath, value);
                OnPropertyChanged(() => TrainingFolderPath);
            }
        }

        private bool _isTrained;

        public bool IsTrained
        {
            get { return _isTrained; }
            set
            {
                SetProperty(ref _isTrained, value);
                OnPropertyChanged(() => IsTrained);
            }
        }

        private string _trainingLog;

        public string TrainingLog
        {
            get { return _trainingLog; }
            set
            {
                SetProperty(ref _trainingLog, value);
                OnPropertyChanged(() => TrainingLog);
            }
        }

        private string _inputTagSentence;

        public string InputTagSentence
        {
            get { return _inputTagSentence; }
            set
            {
                SetProperty(ref _inputTagSentence, value);
                OnPropertyChanged(() => InputTagSentence);
            }
        }

        private string _tagLog;

        public string TagLog
        {
            get { return _tagLog; }
            set
            {
                SetProperty(ref _tagLog, value);
                OnPropertyChanged(() => TagLog);
            }
        }

        public ICommand ChooseTrainingFolderCmd { get; set; }
        public ICommand TrainCmd { get; set; }
        public ICommand GetTrainingInfoCmd { get; set; }
        public ICommand GetAllTagsCmd { get; set; }
        public ICommand GetAllWordsCmd { get; set; }
        public ICommand TestSentenceLevelCmd { get; set; }
        public ICommand TestWordLevelCmd { get; set; }
        public ICommand TagSentenceCmd { get; set; }

        #endregion

        #region Initialization

        public TaggerViewModel(MetroWindow window)
        {
            _window = window;
            //adding tagger
            _tagger = new Tagger();
            //adding services
            _service = new TaggerService();

            //assing commands
            ChooseTrainingFolderCmd = new DelegateCommand(ChooseTrainingFolder);
            TrainCmd = new DelegateCommand(Train);
            GetTrainingInfoCmd = new DelegateCommand(GetTrainingInfo);
            GetAllTagsCmd = new DelegateCommand(GetAllTags);
            GetAllWordsCmd = new DelegateCommand(GetAllWords);
            TestSentenceLevelCmd = new DelegateCommand(TestSentenceLevel);
            TestWordLevelCmd = new DelegateCommand(TestWordLevel);
            TagSentenceCmd = new DelegateCommand(TagSentence);

            //get defaul training folder
            SetDefaultTrainingFolder();

            //set lamda
            SetLamdaValues();

            //marked trained as false
            IsTrained = false;
        }

        private void SetLamdaValues()
        {
            double lamda1, lamda2, lamda3;
            double.TryParse(ConfigurationManager.AppSettings["Lamda1"], out lamda1);
            double.TryParse(ConfigurationManager.AppSettings["Lamda2"], out lamda2);
            double.TryParse(ConfigurationManager.AppSettings["Lamda3"], out lamda3);
            _tagger.SetLamda(lamda1, lamda2, lamda3);
        }

        private void SetDefaultTrainingFolder()
        {
            var defaultFolder = AppDomain.CurrentDomain.BaseDirectory;
            defaultFolder += "\\" + ConfigurationManager.AppSettings["DefaultTrainingFolder"];
            TrainingFolderPath = defaultFolder;
        }

        #endregion

        #region Command Functions

        private void ChooseTrainingFolder()
        {
            string folderPath;
            if (_service.ChooseTrainingFolder(TrainingFolderPath, out folderPath))
            {
                TrainingFolderPath = folderPath;
            }
        }

        private async void Train()
        {
            if (!Directory.Exists(TrainingFolderPath))
            {
                ShowMessage("Training folder doesn't exist. Please choose another folder.");
                return;
            }
            try
            {
                const string progressTitle = "Please wait...";
                const string progressTemplate = "Procesing {0}/{1} files...";
                var sw = new Stopwatch();
                sw.Restart();
                var fileArr = Directory.GetFiles(TrainingFolderPath);
                var controller =
                    await _window.ShowProgressAsync(progressTitle, String.Format(progressTemplate, 0, fileArr.Length));
                var cnt = 0;
                foreach (var fileName in fileArr)
                {
                    _tagger.Train(File.ReadAllText(fileName));
                    await Task.Delay(1);
                    cnt++;
                    controller.SetMessage(String.Format(progressTemplate, cnt, fileArr.Length));
                    controller.SetProgress((double) cnt/fileArr.Length);
                }
                await controller.CloseAsync();

                //marked as is trained
                IsTrained = true;

                //save clean training set
                using (var file =
                    new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "CleanTrainingSet.txt"))
                {
                    foreach (var line in _tagger.TrainingSentenceList)
                    {
                        file.WriteLine(line);
                    }
                }

                _elapsedTime = (double) sw.ElapsedMilliseconds/1000;
                sw.Stop();
                TrainingLog = String.Format("Trained with {0} files. Elapsed Time: {1:##.###}s", fileArr.Length,
                    _elapsedTime) + "\n" + GetTrainingInfoMessage();
            }
            catch (Exception ex)
            {
                _tagger.CleanTrainingSet();
                ShowMessage(ex.Message);
            }
        }

        private void GetTrainingInfo()
        {
            TrainingLog = GetTrainingInfoMessage();
        }

        private string GetTrainingInfoMessage()
        {
            var log = "Training Set Info:\n";
            log += String.Format("Total tokens count: {0:##,###}\n", _tagger.WordCount);
            log += String.Format("Total tags count: {0:##,###}\n", _tagger.TagDict.Count);
            log += String.Format("Total distinct words count: {0:##,###}\n", _tagger.WordDict.Count);
            log += String.Format("Total training sentences count: {0:##,###}\n", _tagger.TrainingSentenceList.Count);
            return log;
        }

        private void GetAllTags()
        {
            TrainingLog = String.Join("\n", _tagger.TagList);
        }

        private void GetAllWords()
        {
            TrainingLog = String.Join("\n", _tagger.WordList);
        }

        private async void TestSentenceLevel()
        {
            try
            {
                const string progressTitle = "Please wait...";
                const string progressTemplate = "Procesing {0}/{1} sentences...\n" +
                                                "Correction: {2}/{3}({4:##.###}%)";
                var sw = new Stopwatch();
                sw.Restart();
                var controller =
                    await
                        _window.ShowProgressAsync(progressTitle,
                            String.Format(progressTemplate, 0, _tagger.TrainingSentenceList.Count, 0, 0, 0));
                var correctSCount = 0;
                for (var i = 0; i < _tagger.TrainingSentenceList.Count; i++)
                {
                    if (_tagger.TestSentenceBySentenceLevel(_tagger.TrainingSentenceList[i],
                        _tagger.TrainingTaggedSentenceList[i]))
                    {
                        correctSCount++;
                    }
                    await Task.Delay(1);
                    controller.SetMessage(String.Format(progressTemplate, (i + 1), _tagger.TrainingSentenceList.Count,
                        correctSCount, (i + 1), (double) correctSCount*100/(i + 1)));
                    controller.SetProgress((double) (i + 1)/_tagger.TrainingSentenceList.Count);
                }
                await controller.CloseAsync();

                _elapsedTime = (double) sw.ElapsedMilliseconds/1000;
                sw.Stop();
                TrainingLog = String.Format("Tested with {0} sentences. Elapsed Time: {1:##.###}s\n" +
                                            "Correction: {2}/{3}({4:##.###}%)", _tagger.TrainingSentenceList.Count,
                    _elapsedTime, correctSCount, _tagger.TrainingSentenceList.Count,
                    (double) correctSCount*100/_tagger.TrainingSentenceList.Count);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
            }
        }

        private async void TestWordLevel()
        {
            try
            {
                const string progressTitle = "Please wait...";
                const string progressTemplate = "Procesing {0}/{1} words...\n" +
                                                "Correction: {2}/{3}({4:##.###}%)";
                var sw = new Stopwatch();
                sw.Restart();
                var controller =
                    await
                        _window.ShowProgressAsync(progressTitle,
                            String.Format(progressTemplate, 0, _tagger.WordCount, 0, 0, 0));
                Int64 totalCount = 0;
                Int64 totalCorrectCount = 0;
                for (var i = 0; i < _tagger.TrainingSentenceList.Count; i++)
                {
                    Int64 wrongCount;
                    Int64 correctCount;
                    _tagger.TestSentenceByWordLevel(_tagger.TrainingSentenceList[i],
                        _tagger.TrainingTaggedSentenceList[i], out correctCount, out wrongCount);
                    totalCount += correctCount + wrongCount;
                    totalCorrectCount += correctCount;
                    await Task.Delay(1);
                    controller.SetMessage(String.Format(progressTemplate, totalCount, _tagger.WordCount,
                        totalCorrectCount, totalCount, (double) totalCorrectCount*100/totalCount));
                    controller.SetProgress((double) totalCount/_tagger.WordCount);
                }
                await controller.CloseAsync();

                _elapsedTime = (double) sw.ElapsedMilliseconds/1000;
                sw.Stop();

                TrainingLog = String.Format("Tested with {0} words. Elapsed Time: {1:##.###}s\n" +
                                            "Correction: {2}/{3}({4:##.###}%)", _tagger.WordCount,
                    _elapsedTime, totalCorrectCount, _tagger.WordCount, (double) totalCorrectCount*100/_tagger.WordCount);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
            }
        }

        private void TagSentence()
        {
            if (InputTagSentence == null || String.IsNullOrEmpty(InputTagSentence.Trim()))
            {
                TagLog = "Please input sentence first.";
                return;
            }

            try
            {

                bool isSuccessful;
                string message;
                List<Tag> tagList;
                var taggedSentence = _tagger.GetTaggedSentence(InputTagSentence, out tagList, out isSuccessful, out message);
                if (!isSuccessful)
                {
                    TagLog = message;
                    TagLog += "\nPlease consider to input your input sentence in the proper way." +
                              "\nFor example: " +
                              "\nHe is a good man. => He is a good man ." +
                              "\nHe is a good man, right? => He is a good man , right ?";
                    return;
                }

                TagLog = taggedSentence;
                TagLog += "\n" + _tagger.GetTagDetail(tagList);
                TagLog +=
                    "Note that some versions of the tagged Brown corpus contain combined tags. For instance the word \"wanna\" is tagged VB+TO, since it is a contracted form of the two words, want/VB and to/TO. Also some tags might be negated, for instance \"aren't\" would be tagged \"BER*\", where * signifies the negation. Additionally, tags may have hyphenations: The tag -HL is hyphenated to the regular tags of words in headlines. The tag -TL is hyphenated to the regular tags of words in titles. The hyphenation -NC signifies an emphasized word. Sometimes the tag has a FW- prefix which means foreign word.";
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);

            }
        }
        #endregion

        #region Helpers

        private async void ShowMessage(string message)
        {
            await
                _window.ShowMessageAsync("Notification", message, MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
        }

        #endregion
    }
}