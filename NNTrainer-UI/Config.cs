using Encog.Util.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NNTrainer_UI
{
  public static  class Config
    {
      public static FileInfo BasePath = new FileInfo(@"c:\app\");

        #region "Step1"

        public static FileInfo BaseFile = FileUtil.CombinePath(BasePath, "AutoMPG.csv");
        public static FileInfo ShuffledBaseFile = FileUtil.CombinePath(BasePath, "AutoMPG_Shuffled.csv");

        #endregion


        #region "Step2"
        public static FileInfo TrainingFile = FileUtil.CombinePath(BasePath, "AutoMPG_Train.csv");
        public static FileInfo EvaluateFile = FileUtil.CombinePath(BasePath, "AutoMPG_Eval.csv");
        public static FileInfo CrossValidationFile = FileUtil.CombinePath(BasePath, "AutoMPG_CV.csv");
        #endregion


        #region "Step3"

        public static FileInfo NormalizedTrainingFile = FileUtil.CombinePath(BasePath, "AutoMPG_Train_Norm.csv");
        public static FileInfo NormalizedEvaluateFile = FileUtil.CombinePath(BasePath, "AutoMPG_Eval_Norm.csv");
        public static FileInfo NormalizedCrossValidationFile = FileUtil.CombinePath(BasePath, "AutoMPG_CV_Norm.csv");
        public static FileInfo AnalystFile = FileUtil.CombinePath(BasePath, "AutoMPG_Analyst.ega");

        #endregion


        #region "Step4"

        public static FileInfo TrainedNetworkFile = FileUtil.CombinePath(BasePath, "AutoMPG_Train.eg");

        #endregion


    }
}
