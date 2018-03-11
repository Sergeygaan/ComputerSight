using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        //Объявление всех переменных, векторов и хааркаскадов
        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels= new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;

        //Класс для определение имени по лицу
        private EigenObjectRecognizer _recognizer = new EigenObjectRecognizer();


        public FrmPrincipal()
        {
            InitializeComponent();
            //Загрузка HaarCascade для обнаружения лиц
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            //eye = new HaarCascade("haarcascade_eye.xml");
            try
            {
                //Загрузка сохраненых изображений
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels+1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }
            
            }
            catch(Exception e)
            {
                //MessageBox.Show(e.ToString());
                MessageBox.Show("Nothing in binary database, please add at least a face(Simply train the prototype with the Add Face Button).", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Инициализация камеры
            grabber = new Capture();
            grabber.QueryFrame();

            //Инициализация события  FrameGrabber
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;
        }


        private void button2_Click(object sender, System.EventArgs e)
        {
            try
            {
                //Количество опознаных лиц
                ContTrain = ContTrain + 1;

                //Преопбазуем полученное изображение с камеры в серые тона
                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                //Распознание лиц
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                7,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                //Действие для каждого обнаруженного лица
                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //Преобразование обнаруженных лиц и тестовых к одному и томуже разрешению, чтобы сравнить один и метод кубической интерполяции
                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox1.Text);

                //Показать лицо в сером формате
                imageBox1.Image = TrainedFace;

                //Сохраним полученное имя в файл. Для дальнейшей работы
                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show(textBox1.Text + "´s face detected and added :)", "Training OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void FrameGrabber(object sender, EventArgs e)
        {
            label3.Text = "0";
            //label4.Text = "";
            NamePersons.Add("");


            //Получение текущего изображение с камеры
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);


            //Преобразование в серые цвета

            gray = currentFrame.Convert<Gray, Byte>();

            //Распознание лиц
            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                 
                face,
                1.2,
                5,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

            //Действие для каждого обнаруженного элемента
            foreach (MCvAvgComp CurrentFace in facesDetected[0])
            {
                t = t + 1;

                result = currentFrame.Copy(CurrentFace.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                //Выделение найденного лица
                currentFrame.Draw(CurrentFace.rect, new Bgr(Color.Red), 1);

                if (trainingImages.ToArray().Length != 0)
                {
                    //Определение лиц с по обученным изображениям
                    MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.0001);

                    ////Распознание лиц
                    //EigenObjectRecognizer recognizer = new EigenObjectRecognizer();

                    _recognizer.EigenObjectRecognizerAdd(
                        trainingImages.ToArray(),
                        labels.ToArray(),
                        3000,
                        ref termCrit);

                    name = _recognizer.Recognize(result);

                    //Вывод имени опознаного лица
                    currentFrame.Draw(name, ref font, new Point(CurrentFace.rect.X - 2, CurrentFace.rect.Y - 2), new Bgr(Color.LightGreen));
                }


                NamePersons[t-1] = name;
                NamePersons.Add("");

                //Количество обнаруженых лиц
                label3.Text = facesDetected[0].Length.ToString();

                /*
                //Set the region of interest on the faces

                gray.ROI = f.rect;
                MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                   eye,
                   1.1,
                   10,
                   Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                   new Size(20, 20));
                gray.ROI = Rectangle.Empty;

                foreach (MCvAvgComp ey in eyesDetected[0])
                {
                    Rectangle eyeRect = ey.rect;
                    eyeRect.Offset(f.rect.X, f.rect.Y);
                    currentFrame.Draw(eyeRect, new Bgr(Color.Blue), 2);
                }
                 */
            }

            t = 0;

            //Вывести имена найденых лиц
            for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
            {
                names = names + NamePersons[nnn] + ", ";
            }

            //Вывод результата
            imageBoxFrameGrabber.Image = currentFrame;
            label4.Text = names;
            names = "";

            //Отчистить список имен
            NamePersons.Clear();
        }
    }
}