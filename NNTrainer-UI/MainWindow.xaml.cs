using Encog;
using Encog.App.Analyst;
using Encog.App.Analyst.CSV.Normalize;
using Encog.App.Analyst.CSV.Segregate;
using Encog.App.Analyst.CSV.Shuffle;
using Encog.App.Analyst.Wizard;
using Encog.Engine.Network.Activation;
using Encog.ML.Data;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Neural.Pattern;
using Encog.Neural.Prune;
using Encog.Persist;
using Encog.Util.CSV;
using Encog.Util.Simple;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace NNTrainer_UI
{

    /// <summary>
    /// NOTE : SET THE BASE FOLDER PATH FOR AutoMPG.CSV and other related files in Config.cs before running this project
    /// </summary>

    public class IterationData
    {
        public int IterationNumber { get; set; }
        public double IterationError { get; set; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IStatusReportable, INotifyPropertyChanged
    {
        #region "Properties"
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<String> _Logs;

        public ObservableCollection<String> Logs
        {
            get { return _Logs; }
            set
            {
                _Logs = value;
                OnPropertyChanged("Logs");


            }
        }


        private ObservableCollection<IterationData> _IterationDataCollection;
        public ObservableCollection<IterationData> IterationDataCollection
        {
            get { return _IterationDataCollection; }
            set
            {
                _IterationDataCollection = value;
                OnPropertyChanged("IterationDataCollection");
            }
        }

        private ObservableCollection<IterationData> _CVIterationDataCollection;
        public ObservableCollection<IterationData> CVIterationDataCollection
        {
            get { return _CVIterationDataCollection; }
            set
            {
                _CVIterationDataCollection = value;
                OnPropertyChanged("CVIterationDataCollection");
            }
        }
        
        private ObservableCollection<String> _IterationLogs;
        public ObservableCollection<String> IterationLogs
        {
            get { return _IterationLogs; }
            set
            {
                _IterationLogs = value;
                OnPropertyChanged("IterationLogs");
            }
        }
        #endregion

        


        BackgroundWorker pruneWorker;
        BackgroundWorker trainWorker;
        BasicNetwork network;

        public MainWindow()
        {
            InitializeComponent();


            //Other Logs
            Logs = new ObservableCollection<string>();
            LstLog.ItemsSource = Logs;


            //Prune Worker
            pruneWorker = new BackgroundWorker();
            pruneWorker.RunWorkerCompleted += pruneWorker_RunWorkerCompleted;
            pruneWorker.DoWork += pruneWorker_DoWork;

        
            //Train Worker
            trainWorker = new BackgroundWorker();
            trainWorker.WorkerReportsProgress = true;
            trainWorker.WorkerSupportsCancellation = true;
            trainWorker.DoWork += trainWorker_DoWork;
            trainWorker.RunWorkerCompleted += trainWorker_RunWorkerCompleted;
            trainWorker.ProgressChanged += trainWorker_ProgressChanged;
           
            
            //Error Series
            IterationDataCollection = new ObservableCollection<IterationData>();
            ErrorSeries.ItemsSource = IterationDataCollection;

            //CV error Series
            CVIterationDataCollection = new ObservableCollection<IterationData>();
            CVErrorSeries.ItemsSource = CVIterationDataCollection;

            //IterationLogs
            IterationLogs = new ObservableCollection<String>();
            LstTraining.ItemsSource = IterationLogs;

        }

        #region "EventHandler"
       
        private void btnPrune_Click(object sender, RoutedEventArgs e)
        {
            Step1(); // Shuffle Step            
            Step2(); //Segregate Step                           
            Step3(); //Normalize Step
            Step4_Pruning(); //Create Network using Pruning
   
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {

            Step1(); // Shuffle Step            
            Step2(); //Segregate Step                           
            Step3(); //Normalize Step
            Step4(); // Create network without pruning
            Step5(); //Training Step
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            trainWorker.CancelAsync();
        }

        #endregion

       

        #region "Step1 : Shuffle"

        private void Step1()
        {
            txtStatus.Text = "Step 1: Shuffle CSV Data File";
            Shuffle(Config.BaseFile);
        }
        private void Shuffle(FileInfo source)
        {
            //Shuffle the CSV File
            var shuffle = new ShuffleCSV();
            shuffle.Analyze(source, true, CSVFormat.English);
            shuffle.ProduceOutputHeaders = true;
            shuffle.Process(Config.ShuffledBaseFile);

        }

        #endregion

        #region "Step2 : Segregate"

        private void Step2()
        {
            txtStatus.Text = "Step 2: Generate training and Evaluation  file";
            Segregate(Config.ShuffledBaseFile);
        }

        private void Segregate(FileInfo source)
        {
            //Segregate source file into training,validation and evaluation file
            var seg = new SegregateCSV();
            seg.Targets.Add(new SegregateTargetPercent(Config.TrainingFile, 60));
            seg.Targets.Add(new SegregateTargetPercent(Config.CrossValidationFile, 20));
            seg.Targets.Add(new SegregateTargetPercent(Config.EvaluateFile, 20));
            seg.ProduceOutputHeaders = true;
            seg.Analyze(source, true, CSVFormat.English);
            seg.Process();
            
        }

        #endregion

        #region "Step3 : Normalize"
        private void Step3()
        {
            txtStatus.Text = "Step 3: Normalize Training and Evaluation Data";
            Normalize();
        }

        private void Normalize()
        {
            //Analyst
            var analyst = new EncogAnalyst();
            //Wizard
            var wizard = new AnalystWizard(analyst);
            wizard.Wizard(Config.BaseFile, true, AnalystFileFormat.DecpntComma);
            //Cylinders
            analyst.Script.Normalize.NormalizedFields[0].Action = Encog.Util.Arrayutil.NormalizationAction.Equilateral;
            //displacement
            analyst.Script.Normalize.NormalizedFields[1].Action = Encog.Util.Arrayutil.NormalizationAction.Normalize;
            //HorsePower
            analyst.Script.Normalize.NormalizedFields[2].Action = Encog.Util.Arrayutil.NormalizationAction.Normalize;
            //weight
            analyst.Script.Normalize.NormalizedFields[3].Action = Encog.Util.Arrayutil.NormalizationAction.Normalize;
            //Acceleration
            analyst.Script.Normalize.NormalizedFields[4].Action = Encog.Util.Arrayutil.NormalizationAction.Normalize;
            //year
            analyst.Script.Normalize.NormalizedFields[5].Action = Encog.Util.Arrayutil.NormalizationAction.Equilateral;
            //Origin
            analyst.Script.Normalize.NormalizedFields[6].Action = Encog.Util.Arrayutil.NormalizationAction.Equilateral;
            //Name
            analyst.Script.Normalize.NormalizedFields[7].Action = Encog.Util.Arrayutil.NormalizationAction.Ignore;
            //mpg
            analyst.Script.Normalize.NormalizedFields[8].Action = Encog.Util.Arrayutil.NormalizationAction.Normalize;
            //Norm for Trainng
            var norm = new AnalystNormalizeCSV();
            norm.ProduceOutputHeaders = true;
            norm.Analyze(Config.TrainingFile, true, CSVFormat.English, analyst);
            norm.Normalize(Config.NormalizedTrainingFile);

            //Norm of Cross Validation
            norm.Analyze(Config.CrossValidationFile, true, CSVFormat.English, analyst);
            norm.Normalize(Config.NormalizedCrossValidationFile);

            //Norm of evaluation
            norm.Analyze(Config.EvaluateFile, true, CSVFormat.English, analyst);
            norm.Normalize(Config.NormalizedEvaluateFile);

          



            //save the analyst file
            analyst.Save(Config.AnalystFile);
        }
        #endregion

        #region "Step4: Create Network using Pruning"

        PruneIncremental prune;

        private void Step4_Pruning()
        {
            txtStatus.Text = "Prune the network";
            var trainingSet = EncogUtility.LoadCSV2Memory(Config.NormalizedTrainingFile.ToString(),
               22, 1, true, CSVFormat.English, false);

            var pattern = new FeedForwardPattern()
            {
                InputNeurons = 22,
                OutputNeurons = 1,
                ActivationFunction = new ActivationTANH()
            };
            prune = new PruneIncremental(trainingSet, pattern, 100, 1, 10, this);
            prune.AddHiddenLayer(1, 10);
            prune.AddHiddenLayer(0, 10);

            Logs.Clear();
            pruneWorker.RunWorkerAsync();
        }

        public void Report(int total, int current, string message)
        {
            Action LogUpdate = () =>
           {
               Logs.Add(String.Format("Logs Message : {0}", message));
           };

            App.Current.Dispatcher.Invoke(LogUpdate, DispatcherPriority.Normal);

        }

        void pruneWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            prune.Process();
        }


        void pruneWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtStatus.Text = "Pruning Completed";
            network = (BasicNetwork)prune.BestNetwork;
            EncogDirectoryPersistence.SaveObject(Config.TrainedNetworkFile, (BasicNetwork)network);
            Step5(); //Train Step
        }

        #endregion


        #region "Step4 : Create Network without Pruning"

        private void Step4()
        {
            txtStatus.Text = "Step 4: Create Neural Network";
            CreateNetwork(Config.TrainedNetworkFile);
        }

        private void CreateNetwork(FileInfo networkFile)
        {
          
                network = new BasicNetwork();
                network.AddLayer(new BasicLayer(new ActivationLinear(), true, 22));
                network.AddLayer(new BasicLayer(new ActivationTANH(), true,6));
                network.AddLayer(new BasicLayer(new ActivationTANH(), false, 1));

                network.Structure.FinalizeStructure();
                network.Reset();
                EncogDirectoryPersistence.SaveObject(networkFile, (BasicNetwork)network);
            
        }

        #endregion


        #region "Step5 : Train Network"
        private void Step5()
        {
            txtStatus.Text = "Step 5: Train Neural Network";
            TrainNetwork();
        }
        IMLDataSet trainingSet;
        IMLDataSet crossValidationSet;
       
        private void TrainNetwork()
        {
            network = (BasicNetwork)EncogDirectoryPersistence.LoadObject(Config.TrainedNetworkFile);
            trainingSet = EncogUtility.LoadCSV2Memory(Config.NormalizedTrainingFile.ToString(),
                 network.InputCount, network.OutputCount, true, CSVFormat.English, false);
            crossValidationSet = EncogUtility.LoadCSV2Memory(Config.NormalizedCrossValidationFile.ToString(),
                network.InputCount, network.OutputCount, true, CSVFormat.English, false);


            train = new ResilientPropagation(network, trainingSet);

            IterationDataCollection.Clear();
            CVIterationDataCollection.Clear();
            IterationLogs.Clear();
            trainWorker.RunWorkerAsync();

        }
        
        private void Wait(double MilliSeconds)
        {
            var frame = new DispatcherFrame();
            new System.Threading.Thread((System.Threading.ThreadStart)(() =>
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(MilliSeconds));
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }


        IMLTrain train;

        void trainWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int epoch = 1;
            do
            {
                if (trainWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
                else 
                {
                    Wait(100);
                    train.Iteration();
                    var IterationResults = new KeyValuePair<int, double>[2];
                    IterationResults[0] = new KeyValuePair<int, double>(epoch, train.Error); ;
                    IterationResults[1] = new KeyValuePair<int, double>(epoch, network.CalculateError(crossValidationSet));
                    trainWorker.ReportProgress(epoch, IterationResults);
                    epoch++;

                }

            } while (train.Error > 0.01);
           
        }

        void trainWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Action UpdateCollection = () =>
            {
                var IterationResults = (KeyValuePair<int, double>[])e.UserState;

                IterationDataCollection.Add(new IterationData() {
                    IterationError = IterationResults[0].Value, IterationNumber = IterationResults[0].Key });
                CVIterationDataCollection.Add(new IterationData() { 
                    IterationError = IterationResults[1].Value, IterationNumber = IterationResults[1].Key });
                IterationLogs.Add(String.Format(IterationResults[0].Value + " Epoch-> " + IterationResults[0].Key));
         
            };
            App.Current.Dispatcher.Invoke(UpdateCollection, DispatcherPriority.Normal);
           
        }

        void trainWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                txtStatus.Text = "Cancelled";
            }
            else
            {
                txtStatus.Text = "Finished";
                EncogDirectoryPersistence.SaveObject(Config.TrainedNetworkFile, (BasicNetwork)network);
            }
        }


        #endregion

       

   
    }
}
